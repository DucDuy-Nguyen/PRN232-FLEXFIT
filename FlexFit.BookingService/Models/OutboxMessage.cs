using System;

namespace FlexFit.BookingService.Models
{
    public class OutboxMessage
    {
        public Guid OutboxMessageId { get; set; }
        public string EventType { get; set; } = null!;
        public string AggregateType { get; set; } = null!;
        public Guid AggregateId { get; set; }
        public string Payload { get; set; } = null!;
        public string? CorrelationId { get; set; }
        public DateTime OccurredAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public int RetryCount { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
