using FlexFit.Booking.Repository.Models;
using FlexFit.Booking.Repository.Repositories.Interfaces;
using FlexFit.Booking.Service.Helpers;
using FlexFit.Booking.Service.Messaging.Events;
using FlexFit.Booking.Service.Service.Interfaces;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexFit.Booking.Service.Service
{
    public class BookingMaintenanceService : IBookingMaintenanceService
    {
        private readonly IBookingRepository _bookingRepository;

        public BookingMaintenanceService(IBookingRepository bookingRepository)
        {
            _bookingRepository = bookingRepository;
        }

        public async Task ProcessExpirationsAsync()
        {
            var now = DateTimeHelper.GetVietnamTime();
            var expirationTime = now.AddMinutes(-5); // Timeout: 5 minutes of PendingPayment

            bool hasChanges = false;

            // 1. GYM BOOKINGS
            var expiredGym = await _bookingRepository.GetGymBookingsForAutoCancellationAsync(expirationTime);

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

                await _bookingRepository.UpdateGymBookingAsync(booking);
                await _bookingRepository.AddOutboxMessageAsync(outbox);
                hasChanges = true;
            }

            // 2. CLASS BOOKINGS
            var expiredClass = await _bookingRepository.GetClassBookingsForAutoCancellationAsync(expirationTime);

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

                await _bookingRepository.UpdateClassBookingAsync(booking);
                await _bookingRepository.AddOutboxMessageAsync(outbox);
                hasChanges = true;
            }

            if (hasChanges)
            {
                await _bookingRepository.SaveChangesAsync();
            }
        }
    }
}
