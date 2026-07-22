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
    public class BookingPaymentHandler : IBookingPaymentHandler
    {
        private readonly IBookingRepository _bookingRepository;
        private readonly ILogger<BookingPaymentHandler> _logger;

        public BookingPaymentHandler(IBookingRepository bookingRepository, ILogger<BookingPaymentHandler> logger)
        {
            _bookingRepository = bookingRepository;
            _logger = logger;
        }

        public async Task HandlePaymentCompletedAsync(CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63 message, Guid eventId)
        {
            var consumerName = "CreditDeductionCompletedConsumer";
            _logger.LogInformation("Processing CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63 for booking {BookingId}", message.BookingId);

            var alreadyProcessed = await _bookingRepository.InboxMessageExistsAsync(eventId, consumerName);
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
                var gymBooking = await _bookingRepository.GetGymBookingByIdAsync(message.BookingId);
                if (gymBooking != null && gymBooking.Status == "PendingPayment")
                {
                    gymBooking.Status = "Confirmed";
                    gymBooking.UpdatedAt = DateTime.UtcNow;
                    bookingCode = gymBooking.BookingCode;
                    userId = gymBooking.UserId;
                    itemTitle = gymBooking.SessionNameSnapshot;
                    updated = true;
                    await _bookingRepository.UpdateGymBookingAsync(gymBooking);
                }
            }
            else if (message.BookingType == "CLASS")
            {
                var classBooking = await _bookingRepository.GetClassBookingByIdAsync(message.BookingId);
                if (classBooking != null && classBooking.Status == "PendingPayment")
                {
                    classBooking.Status = "Confirmed";
                    classBooking.UpdatedAt = DateTime.UtcNow;
                    bookingCode = classBooking.BookingCode;
                    userId = classBooking.UserId;
                    itemTitle = classBooking.ClassNameSnapshot;
                    updated = true;
                    await _bookingRepository.UpdateClassBookingAsync(classBooking);
                }
            }

            if (updated)
            {
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

                await _bookingRepository.AddOutboxMessageAsync(outbox);
                _logger.LogInformation("Booking {BookingId} successfully confirmed. Outbox message queued.", message.BookingId);
            }

            var inbox = new InboxMessage
            {
                EventId = eventId,
                EventType = typeof(CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63).Name,
                ConsumerName = consumerName,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            await _bookingRepository.AddInboxMessageAsync(inbox);
            await _bookingRepository.SaveChangesAsync();
        }

        public async Task HandlePaymentFailedAsync(CreditDeductionFailedEvent message, Guid eventId)
        {
            var consumerName = "CreditDeductionFailedConsumer";
            _logger.LogWarning("Processing CreditDeductionFailedEvent for booking {BookingId}. Reason: {Reason}", message.BookingId, message.Reason);

            var alreadyProcessed = await _bookingRepository.InboxMessageExistsAsync(eventId, consumerName);
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
                var gymBooking = await _bookingRepository.GetGymBookingByIdAsync(message.BookingId);
                if (gymBooking != null && gymBooking.Status == "PendingPayment")
                {
                    gymBooking.Status = "Failed";
                    gymBooking.CancelledAt = DateTime.UtcNow;
                    gymBooking.UpdatedAt = DateTime.UtcNow;
                    userId = gymBooking.UserId;
                    bookingCode = gymBooking.BookingCode;
                    updated = true;
                    await _bookingRepository.UpdateGymBookingAsync(gymBooking);
                }
            }
            else if (message.BookingType == "CLASS")
            {
                var classBooking = await _bookingRepository.GetClassBookingByIdAsync(message.BookingId);
                if (classBooking != null && classBooking.Status == "PendingPayment")
                {
                    classBooking.Status = "Failed";
                    classBooking.CancelledAt = DateTime.UtcNow;
                    classBooking.UpdatedAt = DateTime.UtcNow;
                    userId = classBooking.UserId;
                    bookingCode = classBooking.BookingCode;
                    updated = true;
                    await _bookingRepository.UpdateClassBookingAsync(classBooking);
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

                await _bookingRepository.AddOutboxMessageAsync(outbox);
            }

            var inbox = new InboxMessage
            {
                EventId = eventId,
                EventType = typeof(CreditDeductionFailedEvent).Name,
                ConsumerName = consumerName,
                ReceivedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow
            };

            await _bookingRepository.AddInboxMessageAsync(inbox);
            await _bookingRepository.SaveChangesAsync();
        }
    }
}
