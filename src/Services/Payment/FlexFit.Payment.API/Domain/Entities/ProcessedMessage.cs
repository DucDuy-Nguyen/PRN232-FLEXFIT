using System;

namespace FlexFit.Payment.API.Domain.Entities
{
    public class ProcessedMessage
    {
        public Guid MessageId { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
