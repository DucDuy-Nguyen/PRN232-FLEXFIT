using FlexFit.Contracts.Events;
using FlexFit.Engagement.Application.Interfaces;
using FlexFit.Engagement.Infrastructure.Redis;
using StackExchange.Redis;
using System.Text.Json;

namespace FlexFit.Engagement.API.BackgroundServices;

public class RedisSubscriberBackgroundService : BackgroundService
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

        try
        {
            var subscriber = _redis.GetSubscriber();

            // 1. GymBookingCreated
            await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.GymBookingCreated), async (channel, message) =>
            {
                _logger.LogInformation("Received Redis message from {Channel}: {Message}", channel.ToString(), message.ToString());
                try
                {
                    var evt = JsonSerializer.Deserialize<GymBookingCreatedEvent>(message!);
                    if (evt == null) return;

                    using var scope = _scopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notificationService.SendAsync(
                        evt.UserId,
                        "Đặt lịch thành công!",
                        $"Bạn đã đặt lịch tại {evt.GymName ?? "phòng tập"} - {evt.BranchName ?? "chi nhánh"} vào ngày {evt.BookingDate:dd/MM/yyyy}.",
                        "BookingSuccess");

                    _logger.LogInformation("Processed BookingCreated event for user {UserId}", evt.UserId);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error processing BookingCreated event"); }
            });
            _logger.LogInformation("Subscribed successfully to Redis channel: {Channel}", RedisChannelNames.GymBookingCreated);

            // 2. BookingCancelled
            await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.BookingCancelled), async (channel, message) =>
            {
                _logger.LogInformation("Received Redis message from {Channel}: {Message}", channel.ToString(), message.ToString());
                try
                {
                    var evt = JsonSerializer.Deserialize<BookingCancelledEvent>(message!);
                    if (evt == null) return;

                    using var scope = _scopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notificationService.SendAsync(
                        evt.UserId,
                        "Lịch đặt đã được hủy",
                        $"Lịch đặt {evt.BookingType} của bạn đã bị hủy.{(string.IsNullOrEmpty(evt.Reason) ? "" : $" Lý do: {evt.Reason}")}",
                        "BookingCancelled");

                    _logger.LogInformation("Processed BookingCancelled event for user {UserId}", evt.UserId);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error processing BookingCancelled event"); }
            });
            _logger.LogInformation("Subscribed successfully to Redis channel: {Channel}", RedisChannelNames.BookingCancelled);

            // 3. PaymentCompleted
            await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.PaymentCompleted), async (channel, message) =>
            {
                _logger.LogInformation("Received Redis message from {Channel}: {Message}", channel.ToString(), message.ToString());
                try
                {
                    var evt = JsonSerializer.Deserialize<PaymentCompletedEvent>(message!);
                    if (evt == null) return;

                    using var scope = _scopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notificationService.SendAsync(
                        evt.UserId,
                        "Thanh toán thành công!",
                        $"Bạn đã thanh toán {evt.Amount:N0}đ qua {evt.PaymentMethod}.{(string.IsNullOrEmpty(evt.Description) ? "" : $" {evt.Description}")}",
                        "PaymentSuccess");

                    _logger.LogInformation("Processed PaymentCompleted event for user {UserId}", evt.UserId);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error processing PaymentCompleted event"); }
            });
            _logger.LogInformation("Subscribed successfully to Redis channel: {Channel}", RedisChannelNames.PaymentCompleted);

            // 4. UserRegistered
            await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.UserRegistered), async (channel, message) =>
            {
                _logger.LogInformation("Received Redis message from {Channel}: {Message}", channel.ToString(), message.ToString());
                try
                {
                    var evt = JsonSerializer.Deserialize<UserRegisteredEvent>(message!);
                    if (evt == null) return;

                    using var scope = _scopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notificationService.SendAsync(
                        evt.UserId,
                        "Chào mừng bạn đến với FlexFit!",
                        $"Xin chào {evt.FullName}, tài khoản của bạn đã được tạo thành công. Hãy bắt đầu hành trình sức khỏe ngay!",
                        "AccountUpdate");

                    _logger.LogInformation("Processed UserRegistered event for user {UserId}", evt.UserId);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error processing UserRegistered event"); }
            });
            _logger.LogInformation("Subscribed successfully to Redis channel: {Channel}", RedisChannelNames.UserRegistered);

            // 5. CheckInCompleted
            await subscriber.SubscribeAsync(RedisChannel.Literal(RedisChannelNames.CheckInCompleted), async (channel, message) =>
            {
                _logger.LogInformation("Received Redis message from {Channel}: {Message}", channel.ToString(), message.ToString());
                try
                {
                    var evt = JsonSerializer.Deserialize<CheckInCompletedEvent>(message!);
                    if (evt == null) return;

                    using var scope = _scopeFactory.CreateScope();
                    var notificationService = scope.ServiceProvider.GetRequiredService<INotificationService>();
                    await notificationService.SendAsync(
                        evt.UserId,
                        "Check-in thành công!",
                        $"Bạn đã check-in tại {evt.BranchName ?? "chi nhánh"}. Chúc bạn buổi tập hiệu quả!",
                        "SystemAlert");

                    _logger.LogInformation("Processed CheckInCompleted event for user {UserId}", evt.UserId);
                }
                catch (Exception ex) { _logger.LogError(ex, "Error processing CheckInCompleted event"); }
            });
            _logger.LogInformation("Subscribed successfully to Redis channel: {Channel}", RedisChannelNames.CheckInCompleted);

            // Keep the background service alive indefinitely
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Redis subscriber is stopping.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Redis subscriber failed.");
        }
    }
}
