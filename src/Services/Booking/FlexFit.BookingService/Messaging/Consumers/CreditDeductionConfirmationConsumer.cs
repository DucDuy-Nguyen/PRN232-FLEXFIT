using FlexFit.BookingService.Data;
using FlexFit.BookingService.Messaging.Events;
using FlexFit.BookingService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexFit.BookingService.Messaging.Consumers
{
    public class CreditDeductionCompletedConsumer : IConsumer<CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63>
    {
        private readonly BookingDbContext _dbContext;
        private readonly ILogger<CreditDeductionCompletedConsumer> _logger;

        public CreditDeductionCompletedConsumer(BookingDbContext dbContext, ILogger<CreditDeductionCompletedConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63> context)
        {
            var message = context.Message;
            var eventId = context.MessageId ?? Guid.NewGuid();
            var consumerName = nameof(CreditDeductionCompletedConsumer);

            _logger.LogInformation("Processing CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63 for booking {BookingId}", message.BookingId);

            // Inbox Pattern: Idempotency Check
            var alreadyProcessed = await _dbContext.InboxMessages
                .AnyAsync(i => i.EventId == eventId && i.ConsumerName == consumerName);

            if (alreadyProcessed)
            {
                _logger.LogWarning("Event {EventId} has already been processed by {ConsumerName}", eventId, consumerName);
                return;
            }

            bool updated = false;
            string bookingCode = "";
            Guid userId = Guid.Empty;
            string itemTitle = "";

            if (message.BookingType == "GYM")
            {
                var gymBooking = await _dbContext.GymBookings.FirstOrDefaultAsync(b => b.BookingId == message.BookingId);
                if (gymBooking != null && gymBooking.Status == "PendingPayment")
                {
                    gymBooking.Status = "Confirmed";
                    gymBooking.UpdatedAt = DateTime.UtcNow;
                    bookingCode = gymBooking.BookingCode;
                    userId = gymBooking.UserId;
                    itemTitle = gymBooking.SessionNameSnapshot;
                    updated = true;
                }
            }
            else if (message.BookingType == "CLASS")
            {
                var classBooking = await _dbContext.ClassBookings.FirstOrDefaultAsync(b => b.BookingId == message.BookingId);
                if (classBooking != null && classBooking.Status == "PendingPayment")
                {
                    classBooking.Status = "Confirmed";
                    classBooking.UpdatedAt = DateTime.UtcNow;
                    bookingCode = classBooking.BookingCode;
                    userId = classBooking.UserId;
                    itemTitle = classBooking.ClassNameSnapshot;
                    updated = true;
                }
            }

            if (updated)
            {
                // Enqueue Notification to Outbox
                var notification = new NotificationRequestedEvent
                {
                    UserId = userId,
                    Title = "Đặt lịch thành công 🎉",
                    Message = $"Chúc mừng bạn đã đặt lịch tập cho [{itemTitle}] thành công. Mã đặt lịch: {bookingCode}.",
                    Type = "BookingConfirmed"
                };

                var outbox = new OutboxMessage
                {
                    OutboxMessageId = Guid.NewGuid(),
                    EventType = typeof(NotificationRequestedEvent).Name,
                    AggregateType = "Booking",
                    AggregateId = message.BookingId,
                    Payload = JsonSerializer.Serialize(notification),
                    CorrelationId = message.BookingId.ToString(),
                    OccurredAt = DateTime.UtcNow
                };

                await _dbContext.OutboxMessages.AddAsync(outbox);
                _logger.LogInformation("Booking {BookingId} successfully confirmed. Outbox message queued.", message.BookingId);
            }

            // Record into Inbox
            var inbox = new InboxMessage
            {
                EventId = eventId,
                EventType = typeof(CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63).Name,
                ConsumerName = consumerName,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            await _dbContext.InboxMessages.AddAsync(inbox);
            await _dbContext.SaveChangesAsync();
        }
    }

    public class CreditDeductionFailedConsumer : IConsumer<CreditDeductionFailedEvent>
    {
        private readonly BookingDbContext _dbContext;
        private readonly ILogger<CreditDeductionFailedConsumer> _logger;

        public CreditDeductionFailedConsumer(BookingDbContext dbContext, ILogger<CreditDeductionFailedConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<CreditDeductionFailedEvent> context)
        {
            var message = context.Message;
            var eventId = context.MessageId ?? Guid.NewGuid();
            var consumerName = nameof(CreditDeductionFailedConsumer);

            _logger.LogWarning("Processing CreditDeductionFailedEvent for booking {BookingId}. Reason: {Reason}", message.BookingId, message.Reason);

            // Inbox Pattern Check
            var alreadyProcessed = await _dbContext.InboxMessages
                .AnyAsync(i => i.EventId == eventId && i.ConsumerName == consumerName);

            if (alreadyProcessed)
            {
                _logger.LogWarning("Event {EventId} has already been processed by {ConsumerName}", eventId, consumerName);
                return;
            }

            bool updated = false;
            Guid userId = Guid.Empty;
            string bookingCode = "";

            if (message.BookingType == "GYM")
            {
                var gymBooking = await _dbContext.GymBookings.FirstOrDefaultAsync(b => b.BookingId == message.BookingId);
                if (gymBooking != null && gymBooking.Status == "PendingPayment")
                {
                    gymBooking.Status = "Failed";
                    gymBooking.CancelledAt = DateTime.UtcNow;
                    gymBooking.UpdatedAt = DateTime.UtcNow;
                    userId = gymBooking.UserId;
                    bookingCode = gymBooking.BookingCode;
                    updated = true;
                }
            }
            else if (message.BookingType == "CLASS")
            {
                var classBooking = await _dbContext.ClassBookings.FirstOrDefaultAsync(b => b.BookingId == message.BookingId);
                if (classBooking != null && classBooking.Status == "PendingPayment")
                {
                    classBooking.Status = "Failed";
                    classBooking.CancelledAt = DateTime.UtcNow;
                    classBooking.UpdatedAt = DateTime.UtcNow;
                    userId = classBooking.UserId;
                    bookingCode = classBooking.BookingCode;
                    updated = true;
                }
            }

            if (updated)
            {
                var notification = new NotificationRequestedEvent
                {
                    UserId = userId,
                    Title = "Đặt lịch thất bại ❌",
                    Message = $"Giao dịch đặt lịch {bookingCode} không hợp lệ hoặc không đủ credit. Chi tiết: {message.Reason}",
                    Type = "BookingFailed"
                };

                var outbox = new OutboxMessage
                {
                    OutboxMessageId = Guid.NewGuid(),
                    EventType = typeof(NotificationRequestedEvent).Name,
                    AggregateType = "Booking",
                    AggregateId = message.BookingId,
                    Payload = JsonSerializer.Serialize(notification),
                    CorrelationId = message.BookingId.ToString(),
                    OccurredAt = DateTime.UtcNow
                };

                await _dbContext.OutboxMessages.AddAsync(outbox);
            }

            // Record into Inbox
            var inbox = new InboxMessage
            {
                EventId = eventId,
                EventType = typeof(CreditDeductionFailedEvent).Name,
                ConsumerName = consumerName,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            await _dbContext.InboxMessages.AddAsync(inbox);
            await _dbContext.SaveChangesAsync();
        }
    }
}
