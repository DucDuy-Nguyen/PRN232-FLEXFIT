using FlexFit.Booking.Repository.Data;
using FlexFit.Booking.Service.Helpers;
using FlexFit.Booking.Service.Messaging.Events;
using FlexFit.Booking.Repository.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Booking.API.BackgroundJobs
{
    public class AutoCancelExpiredBookingJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AutoCancelExpiredBookingJob> _logger;

        public AutoCancelExpiredBookingJob(IServiceScopeFactory scopeFactory, ILogger<AutoCancelExpiredBookingJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Flexfit Auto Cancel Expired Booking Job initialized.");

            using var timer = new PeriodicTimer(TimeSpan.FromMinutes(1)); // Runs scan every 1 minute

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessExpirationsAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in automatic Expired Booking Cancellation scan.");
                }
            }
        }

        private async Task ProcessExpirationsAsync()
        {
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();

            var now = DateTimeHelper.GetVietnamTime();
            var expirationTime = now.AddMinutes(-5); // Timeout: 5 minutes of PendingPayment

            bool hasChanges = false;

            // 1. GYM BOOKINGS
            var expiredGym = await dbContext.GymBookings
                .Where(b => b.Status == "PendingPayment" && b.BookedAt <= expirationTime)
                .ToListAsync();

            foreach (var booking in expiredGym)
            {
                booking.Status = "Failed";
                booking.CancelledAt = now;
                booking.ExpiredAt = now;
                booking.UpdatedAt = now;

                var outbox = new OutboxMessage
                {
                    OutboxMessageId = Guid.NewGuid(),
                    EventType = typeof(BookingExpiredEvent).Name,
                    AggregateType = "GymBooking",
                    AggregateId = booking.BookingId,
                    Payload = JsonSerializer.Serialize(new BookingExpiredEvent
                    {
                        BookingId = booking.BookingId,
                        BookingType = "GYM",
                        UserId = booking.UserId,
                        CorrelationId = booking.BookingId
                    }),
                    CorrelationId = booking.BookingId.ToString(),
                    OccurredAt = DateTime.UtcNow
                };

                await dbContext.OutboxMessages.AddAsync(outbox);
                _logger.LogWarning("Auto-cancelled expired gym booking {BookingId} due to payment timeout.", booking.BookingId);
                hasChanges = true;
            }

            // 2. CLASS BOOKINGS
            var expiredClass = await dbContext.ClassBookings
                .Where(b => b.Status == "PendingPayment" && b.BookedAt <= expirationTime)
                .ToListAsync();

            foreach (var booking in expiredClass)
            {
                booking.Status = "Failed";
                booking.CancelledAt = now;
                booking.ExpiredAt = now;
                booking.UpdatedAt = now;

                var outbox = new OutboxMessage
                {
                    OutboxMessageId = Guid.NewGuid(),
                    EventType = typeof(BookingExpiredEvent).Name,
                    AggregateType = "ClassBooking",
                    AggregateId = booking.BookingId,
                    Payload = JsonSerializer.Serialize(new BookingExpiredEvent
                    {
                        BookingId = booking.BookingId,
                        BookingType = "CLASS",
                        UserId = booking.UserId,
                        CorrelationId = booking.BookingId
                    }),
                    CorrelationId = booking.BookingId.ToString(),
                    OccurredAt = DateTime.UtcNow
                };

                await dbContext.OutboxMessages.AddAsync(outbox);
                _logger.LogWarning("Auto-cancelled expired class booking {BookingId} due to payment timeout.", booking.BookingId);
                hasChanges = true;
            }

            if (hasChanges)
            {
                await dbContext.SaveChangesAsync();
            }
        }
    }
}
