using System;

namespace FlexFit.BookingService.Models
{
    public class InboxMessage
    {
        public Guid EventId { get; set; }
        public string EventType { get; set; } = null!;
        public string ConsumerName { get; set; } = null!;
        public DateTime ReceivedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
