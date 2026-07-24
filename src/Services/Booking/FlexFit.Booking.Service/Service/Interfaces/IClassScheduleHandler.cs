using FlexFit.Booking.Service.Messaging.Events;
using System;
using System.Threading.Tasks;

namespace FlexFit.Booking.Service.Service.Interfaces
{
    public interface IClassScheduleHandler
    {
        Task HandleClassScheduleChangedAsync(ClassScheduleChangedEvent message, Guid eventId);
    }
}
