using FlexFit.Booking.Repository.Models;
using FlexFit.Booking.Repository.Repositories.Interfaces;
using FlexFit.Booking.Service.Messaging.Events;
using FlexFit.Booking.Service.Service.Interfaces;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexFit.Booking.Service.Service
{
    public class ClassScheduleHandler : IClassScheduleHandler
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<ClassScheduleHandler> _logger;

        public ClassScheduleHandler(IBookingRepository bookingRepository, ILogger<ClassScheduleHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task HandleClassScheduleChangedAsync(ClassScheduleChangedEvent message, Guid eventId)
        {
            var consumerName = "ClassScheduleChangedConsumer";
            _logger.LogInformation("Processing ClassScheduleChangedEvent for class {ClassId}", message.ClassId);

            var alreadyProcessed = await _bookingRepository.InboxMessageExistsAsync(eventId, consumerName);
            if (alreadyProcessed)
            {
                _logger.LogWarning("Event {EventId} has already been processed by {ConsumerName}", eventId, consumerName);
                return;
            }

            var activeBookings = await _bookingRepository.GetActiveClassBookingsByClassIdAsync(message.ClassId);

            foreach (var booking in activeBookings)
            {
                booking.StartTimeSnapshot = message.NewStartTime;
                booking.EndTimeSnapshot = message.NewEndTime;
                booking.UpdatedAt = DateTime.UtcNow;

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

                await _bookingRepository.UpdateClassBookingAsync(booking);
                await _bookingRepository.AddOutboxMessageAsync(outbox);
            }

            var inbox = new InboxMessage
            {
                EventId = eventId,
                EventType = typeof(ClassScheduleChangedEvent).Name,
                ConsumerName = consumerName,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            await _bookingRepository.AddInboxMessageAsync(inbox);
            await _bookingRepository.SaveChangesAsync();

            _logger.LogInformation("Updated schedules for {Count} active bookings under Class {ClassId}", activeBookings.Count, message.ClassId);
        }
    }
}
