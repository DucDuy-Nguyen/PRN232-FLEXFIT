using FlexFit.Booking.Service.Messaging.Events;
using FlexFit.Booking.Service.Service.Interfaces;
using MassTransit;
using System;
using System.Threading.Tasks;

namespace FlexFit.Booking.API.Messaging.Consumers
{
    public class CreditDeductionCompletedConsumer : IConsumer<CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63>
    {
        private readonly IBookingPaymentHandler _paymentHandler;

        public CreditDeductionCompletedConsumer(IBookingPaymentHandler paymentHandler)
        {
            _paymentHandler = paymentHandler;
        }

        public async Task Consume(ConsumeContext<CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63> context)
        {
            var eventId = context.MessageId ?? Guid.NewGuid();
            await _paymentHandler.HandlePaymentCompletedAsync(context.Message, eventId);
        }
    }

    public class CreditDeductionFailedConsumer : IConsumer<CreditDeductionFailedEvent>
    {
        private readonly IBookingPaymentHandler _paymentHandler;

        public CreditDeductionFailedConsumer(IBookingPaymentHandler paymentHandler)
        {
            _paymentHandler = paymentHandler;
        }

        public async Task Consume(ConsumeContext<CreditDeductionFailedEvent> context)
        {
            var eventId = context.MessageId ?? Guid.NewGuid();
            await _paymentHandler.HandlePaymentFailedAsync(context.Message, eventId);
        }
    }
}
