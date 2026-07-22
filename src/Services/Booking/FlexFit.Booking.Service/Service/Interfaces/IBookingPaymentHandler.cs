using FlexFit.Booking.Service.Messaging.Events;
using System;
using System.Threading.Tasks;

namespace FlexFit.Booking.Service.Service.Interfaces
{
    public interface IBookingPaymentHandler
    {
        Task HandlePaymentCompletedAsync(CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63 message, Guid eventId);
        Task HandlePaymentFailedAsync(CreditDeductionFailedEvent message, Guid eventId);
    }
}
