using FlexFit.Booking.Service.Helpers;
using FlexFit.Booking.Service.Messaging.Events;
using FlexFit.Booking.Repository.Models;
using FlexFit.Booking.Repository.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Booking.API.BackgroundJobs
{
    public class BookingReminderJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<BookingReminderJob> _logger;

        public BookingReminderJob(IServiceScopeFactory scopeFactory, ILogger<BookingReminderJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Flexfit Booking Reminder Job initialized successfully.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(30)); // Runs scan every 30 seconds

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                 {
                    await ProcessRemindersAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in automatic Booking Reminder scan.");
                }
            }
        }

        private async Task ProcessRemindersAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var bookingRepo = scope.ServiceProvider.GetRequiredService<IBookingRepository>();

            var now = DateTimeHelper.GetVietnamTime();
            bool hasChanges = false;

            // 1. GYM BOOKINGS REMINDERS
            var gym3h = await bookingRepo.GetUpcomingUnremindedBookingsAsync(now, hoursLeft: 3);
            foreach (var booking in gym3h.gymBookings)
            {
                var timeDiff = booking.StartTimeSnapshot - now;
                if (timeDiff.TotalHours <= 2.75) continue;

                var outbox = new OutboxMessage
                {
                    OutboxMessageId = Guid.NewGuid(),
                    EventType = typeof(NotificationRequestedEvent).Name,
                    AggregateType = "GymBooking",
                    AggregateId = booking.BookingId,
                    Payload = JsonSerializer.Serialize(new NotificationRequestedEvent
                    {
                        UserId = booking.UserId,
                        Title = "Nhắc nhở lịch tập Gym (3h trước giờ tập) ⏰",
                        Message = $"Lịch đặt tập Gym [{booking.SessionNameSnapshot}] tại [{booking.BranchNameSnapshot}] sẽ bắt đầu lúc {booking.StartTimeSnapshot:HH:mm dd/MM/yyyy}. Mã đơn: {booking.BookingCode} (3h trước).",
                        Type = "BookingReminder3h"
                    }),
                    CorrelationId = booking.BookingId.ToString(),
                    OccurredAt = DateTime.UtcNow
                };

                await bookingRepo.AddOutboxMessageAsync(outbox);
                booking.IsReminded3h = true;
                booking.UpdatedAt = now;
                await bookingRepo.UpdateGymBookingAsync(booking);
                hasChanges = true;
            }

            var gym1h = await bookingRepo.GetUpcomingUnremindedBookingsAsync(now, hoursLeft: 1);
            foreach (var booking in gym1h.gymBookings)
            {
                var timeDiff = booking.StartTimeSnapshot - now;
                if (timeDiff.TotalHours <= 1.0 && timeDiff.TotalHours > 0)
                {
                    var outbox = new OutboxMessage
                    {
                        OutboxMessageId = Guid.NewGuid(),
                        EventType = typeof(NotificationRequestedEvent).Name,
                        AggregateType = "GymBooking",
                        AggregateId = booking.BookingId,
                        Payload = JsonSerializer.Serialize(new NotificationRequestedEvent
                        {
                            UserId = booking.UserId,
                            Title = "Sắp đến giờ tập Gym (1h trước giờ tập) ⏰",
                            Message = $"Lịch đặt tập Gym [{booking.SessionNameSnapshot}] tại [{booking.BranchNameSnapshot}] sẽ bắt đầu lúc {booking.StartTimeSnapshot:HH:mm dd/MM/yyyy}. Mã đơn: {booking.BookingCode} (1h trước).",
                            Type = "BookingReminder1h"
                        }),
                        CorrelationId = booking.BookingId.ToString(),
                        OccurredAt = DateTime.UtcNow
                    };

                    await bookingRepo.AddOutboxMessageAsync(outbox);
                    booking.IsReminded1h = true;
                    booking.IsReminded3h = true;
                    booking.UpdatedAt = now;
                    await bookingRepo.UpdateGymBookingAsync(booking);
                    hasChanges = true;
                }
            }

            // 2. CLASS BOOKINGS REMINDERS
            var class3h = await bookingRepo.GetUpcomingUnremindedBookingsAsync(now, hoursLeft: 3);
            foreach (var booking in class3h.classBookings)
            {
                var timeDiff = booking.StartTimeSnapshot - now;
                if (timeDiff.TotalHours <= 2.75) continue;

                var outbox = new OutboxMessage
                {
                    OutboxMessageId = Guid.NewGuid(),
                    EventType = typeof(NotificationRequestedEvent).Name,
                    AggregateType = "ClassBooking",
                    AggregateId = booking.BookingId,
                    Payload = JsonSerializer.Serialize(new NotificationRequestedEvent
                    {
                        UserId = booking.UserId,
                        Title = "Nhắc nhở lịch học Class (3h trước giờ học) ⏰",
                        Message = $"Lịch đặt lớp [{booking.ClassNameSnapshot}] tại [{booking.BranchNameSnapshot}] sẽ bắt đầu lúc {booking.StartTimeSnapshot:HH:mm dd/MM/yyyy}. Mã đơn: {booking.BookingCode} (3h trước).",
                        Type = "BookingReminder3h"
                    }),
                    CorrelationId = booking.BookingId.ToString(),
                    OccurredAt = DateTime.UtcNow
                };

                await bookingRepo.AddOutboxMessageAsync(outbox);
                booking.IsReminded3h = true;
                booking.UpdatedAt = now;
                await bookingRepo.UpdateClassBookingAsync(booking);
                hasChanges = true;
            }

            var class1h = await bookingRepo.GetUpcomingUnremindedBookingsAsync(now, hoursLeft: 1);
            foreach (var booking in class1h.classBookings)
            {
                var timeDiff = booking.StartTimeSnapshot - now;
                if (timeDiff.TotalHours <= 1.0 && timeDiff.TotalHours > 0)
                {
                    var outbox = new OutboxMessage
                    {
                        OutboxMessageId = Guid.NewGuid(),
                        EventType = typeof(NotificationRequestedEvent).Name,
                        AggregateType = "ClassBooking",
                        AggregateId = booking.BookingId,
                        Payload = JsonSerializer.Serialize(new NotificationRequestedEvent
                        {
                            UserId = booking.UserId,
                            Title = "Sắp đến giờ học Class (1h trước giờ học) ⏰",
                            Message = $"Lịch đặt lớp [{booking.ClassNameSnapshot}] tại [{booking.BranchNameSnapshot}] sẽ bắt đầu lúc {booking.StartTimeSnapshot:HH:mm:ss dd/MM/yyyy}. Mã đơn: {booking.BookingCode} (1h trước).",
                            Type = "BookingReminder1h"
                        }),
                        CorrelationId = booking.BookingId.ToString(),
                        OccurredAt = DateTime.UtcNow
                    };

                    await bookingRepo.AddOutboxMessageAsync(outbox);
                    booking.IsReminded1h = true;
                    booking.IsReminded3h = true;
                    booking.UpdatedAt = now;
                    await bookingRepo.UpdateClassBookingAsync(booking);
                    hasChanges = true;
                }
            }

            if (hasChanges)
            {
                await bookingRepo.SaveChangesAsync();
            }
        }
    }
}
