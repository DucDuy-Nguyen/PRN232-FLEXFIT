using System;

namespace FlexFit.Payment.Repository.Entities
{
    public class ProcessedMessage
    {
        public Guid MessageId { get; set; }
        public DateTime ProcessedAt { get; set; }
    }
}
