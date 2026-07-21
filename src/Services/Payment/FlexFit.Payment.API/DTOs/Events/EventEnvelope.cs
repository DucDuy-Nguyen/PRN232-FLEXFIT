using System;

namespace FlexFit.Payment.API.DTOs.Events
{
    public class EventEnvelope
    {
        public Guid EventId { get; set; } = Guid.NewGuid();
        public string EventType { get; set; } = null!;
        public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
        public Guid CorrelationId { get; set; } = Guid.NewGuid();
        public Guid? CausationId { get; set; }
        public string Producer { get; set; } = "FlexFit.PaymentService";
        public string SchemaVersion { get; set; } = "1.0";
        public string Payload { get; set; } = null!; // JSON representation of the actual event payload
    }
}


