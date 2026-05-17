using System;

namespace Flexfit.DTOs.Payment
{
    public class PaymentResponse
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public Guid PackageId { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? PaymentUrl { get; set; } // URL to redirect user for payment (e.g. VNPAY/MOMO/MOCK)
        public string Status { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
    }
}
