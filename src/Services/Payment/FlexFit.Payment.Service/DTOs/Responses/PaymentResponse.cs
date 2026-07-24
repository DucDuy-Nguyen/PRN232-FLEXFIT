using System;

namespace FlexFit.Payment.Service.DTOs.Responses
{
    public class PaymentResponse
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public Guid PackageId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentUrl { get; set; }
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
