using FlexFit.BookingService.Data;
using FlexFit.BookingService.Messaging.Events;
using FlexFit.BookingService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexFit.BookingService.Messaging.Consumers
{
    public class ClassScheduleChangedConsumer : IConsumer<ClassScheduleChangedEvent>
    {
        private readonly BookingDbContext _dbContext;
        private readonly ILogger<ClassScheduleChangedConsumer> _logger;

        public ClassScheduleChangedConsumer(BookingDbContext dbContext, ILogger<ClassScheduleChangedConsumer> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<ClassScheduleChangedEvent> context)
        {
            var message = context.Message;
            var eventId = context.MessageId ?? Guid.NewGuid();
            var consumerName = nameof(ClassScheduleChangedConsumer);

            _logger.LogInformation("Processing ClassScheduleChangedEvent for class {ClassId}", message.ClassId);

            // Inbox Pattern check
            var alreadyProcessed = await _dbContext.InboxMessages
                .AnyAsync(i => i.EventId == eventId && i.ConsumerName == consumerName);

            if (alreadyProcessed)
            {
                _logger.LogWarning("Event {EventId} has already been processed by {ConsumerName}", eventId, consumerName);
                return;
            }

            // Find all active class bookings for this class
            var activeBookings = await _dbContext.ClassBookings
                .Where(b => b.ClassId == message.ClassId && b.Status == "Confirmed")
                .ToListAsync();

            foreach (var booking in activeBookings)
            {
                booking.StartTimeSnapshot = message.NewStartTime;
                booking.EndTimeSnapshot = message.NewEndTime;
                booking.UpdatedAt = DateTime.UtcNow;

                // Queue outbox notification
                var notification = new NotificationRequestedEvent
                {
                    UserId = booking.UserId,
                    Title = "Thay đổi lịch học lớp 📅",
                    Message = $"Lịch học lớp [{booking.ClassNameSnapshot}] đã được dời lịch sang giờ mới: {message.NewStartTime:HH:mm dd/MM/yyyy}.",
                    Type = "ScheduleChanged"
                };

                var outbox = new OutboxMessage
                {
                    OutboxMessageId = Guid.NewGuid(),
                    EventType = typeof(NotificationRequestedEvent).Name,
                    AggregateType = "ClassBooking",
                    AggregateId = booking.BookingId,
                    Payload = JsonSerializer.Serialize(notification),
                    CorrelationId = booking.BookingId.ToString(),
                    OccurredAt = DateTime.UtcNow
                };

                await _dbContext.OutboxMessages.AddAsync(outbox);
            }

            // Record Inbox Entry
            var inbox = new InboxMessage
            {
                EventId = eventId,
                EventType = typeof(ClassScheduleChangedEvent).Name,
                ConsumerName = consumerName,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            await _dbContext.InboxMessages.AddAsync(inbox);
            await _dbContext.SaveChangesAsync();

            _logger.LogInformation("Updated schedules for {Count} active bookings under Class {ClassId}", activeBookings.Count, message.ClassId);
        }
    }
}
