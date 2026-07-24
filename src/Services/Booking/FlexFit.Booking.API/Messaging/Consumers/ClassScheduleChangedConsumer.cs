using FlexFit.Booking.Service.Messaging.Events;
using FlexFit.Booking.Service.Service.Interfaces;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace FlexFit.Booking.API.Messaging.Consumers
{
    public class ClassScheduleChangedConsumer : IConsumer<ClassScheduleChangedEvent>
    {
        private readonly IClassScheduleHandler _scheduleHandler;

        public ClassScheduleChangedConsumer(IClassScheduleHandler scheduleHandler)
        {
            _scheduleHandler = scheduleHandler;
        }

        public async Task Consume(ConsumeContext<ClassScheduleChangedEvent> context)
        {
            var eventId = context.MessageId ?? Guid.NewGuid();
            await _scheduleHandler.HandleClassScheduleChangedAsync(context.Message, eventId);
        }
    }
}
