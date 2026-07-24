using FlexFit.Engagement.Repository.Data;
using FlexFit.Contracts.Events;
using FlexFit.Engagement.Repository.Repositories.Interfaces;
using FlexFit.Engagement.Repository.Models;
using FlexFit.Engagement.API.Redis;
using FlexFit.Engagement.Service.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System.Text.Json;

namespace FlexFit.Engagement.API.BackgroundServices;

public sealed class RedisSubscriberBackgroundService : BackgroundService
{
    private readonly IConnectionMultiplexer _redis;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RedisSubscriberBackgroundService> _logger;

    public RedisSubscriberBackgroundService(
        IConnectionMultiplexer redis,
        IServiceScopeFactory scopeFactory,
        ILogger<RedisSubscriberBackgroundService> logger)
    {
        _redis = redis;
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Redis Subscriber Background Service is starting...");

        var subscriber = _redis.GetSubscriber();

        // 1. Subscribe to GymBookingCreatedEvent
        await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.BookingCreated), async (channel, message) =>
        {
            try
            {
                var evt = JsonSerializer.Deserialize<GymBookingCreatedEvent>(message!);
                if (evt == null) return;

                _logger.LogInformation("Received BookingCreated event for user {UserId}", evt.UserId);

                using var scope = _scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notificationService.SendAsync(
                    evt.UserId,
                    "Đặt lịch thành công!",
                    $"Bạn đã đặt lịch tại {evt.GymName ?? "phòng tập"} - {evt.BranchName ?? "chi nhánh"} vào ngày {evt.BookingDate:dd/MM/yyyy}.",
                    "BookingSuccess");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing BookingCreated event");
            }
        });

        // 2. Subscribe to BookingCancelledEvent
        await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.BookingCancelled), async (channel, message) =>
        {
            try
            {
                var evt = JsonSerializer.Deserialize<BookingCancelledEvent>(message!);
                if (evt == null) return;

                _logger.LogInformation("Received BookingCancelled event for user {UserId}", evt.UserId);

                using var scope = _scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notificationService.SendAsync(
                    evt.UserId,
                    "Lịch đặt đã được hủy",
                    $"Lịch đặt {evt.BookingType} của bạn đã bị hủy.{(string.IsNullOrEmpty(evt.Reason) ? "" : $" Lý do: {evt.Reason}")}",
                    "BookingCancelled");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing BookingCancelled event");
            }
        });

        // 3. Subscribe to PaymentCompletedEvent
        await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.PaymentCompleted), async (channel, message) =>
        {
            try
            {
                var evt = JsonSerializer.Deserialize<PaymentCompletedEvent>(message!);
                if (evt == null) return;

                _logger.LogInformation("Received PaymentCompleted event for user {UserId}", evt.UserId);

                using var scope = _scopeFactory.CreateScope();
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notificationService.SendAsync(
                    evt.UserId,
                    "Thanh toán thành công!",
                    $"Bạn đã thanh toán {evt.Amount:N0}đ qua {evt.PaymentMethod}.{(string.IsNullOrEmpty(evt.Description) ? "" : $" {evt.Description}")}",
                    "PaymentSuccess");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing PaymentCompleted event");
            }
        });

        // 4. Subscribe to UserRegisteredEvent
        await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.UserRegistered), async (channel, message) =>
        {
            try
            {
                var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(message!);
                if (evt == null) return;

                _logger.LogInformation("Received UserRegistered event for user {UserId} ({FullName})", evt.UserId, evt.FullName);

                using var scope = _scopeFactory.CreateScope();
                
                // Sync User to local Engagement DB to support showing reviewer names, log details etc.
                var userRepo = scope.ServiceProvider.GetRequiredService<IEngagementUserRepository>();
                var existingUser = await userRepo.GetByIdAsync(evt.UserId);
                if (existingUser == null)
                {
                    await userRepo.AddAsync(new User
                    {
                        UserId = evt.UserId,
                        FullName = evt.FullName,
                        Email = evt.Email
                    });
                    await userRepo.SaveChangesAsync();
                }

                // Send Welcome Notification
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notificationService.SendAsync(
                    evt.UserId,
                    "Chào mừng bạn đến với FlexFit!",
                    $"Xin chào {evt.FullName}, tài khoản của bạn đã được tạo thành công. Hãy bắt đầu hành trình sức khỏe ngay!",
                    "AccountUpdate");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing UserRegistered event");
            }
        });

        // 5. Subscribe to CheckInCompletedEvent
        await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.CheckInCompleted), async (channel, message) =>
        {
            try
            {
                var evt = JsonSerializer.Deserialize<CheckInCompletedEvent>(message!);
                if (evt == null) return;

                _logger.LogInformation("Received CheckInCompleted event for user {UserId}", evt.UserId);

                using var scope = _scopeFactory.CreateScope();

                // Create local workout history
                var workoutService = scope.ServiceProvider.GetRequiredService<IWorkoutHistoryService>();
                Guid? classBookingId = evt.BookingType == "Class" ? evt.BookingId : null;
                Guid? gymBookingId = evt.BookingType == "Gym" ? evt.BookingId : null;

                // Estimate calories & duration based on class/session (default to 400 calories, 60 minutes)
                await workoutService.CreateHistoryFromCheckInAsync(evt.UserId, classBookingId, gymBookingId, 400, 60);

                // Send Notification
                var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                await notificationService.SendAsync(
                    evt.UserId,
                    "Check-in thành công!",
                    $"Bạn đã check-in thành công tại {evt.BranchName ?? "chi nhánh phòng tập"}. Chúc bạn buổi tập hiệu quả!",
                    "SystemAlert");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing CheckInCompleted event");
            }
        });

        // Maintain the background loop
        while (!stoppingToken.IsCancellationRequested)
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
    }
}
