using System;

namespace Flexfit.DTOs.Payment
{
    public class CreatePaymentRequest
    {
        public Guid PackageId { get; set; }
        public string PaymentMethod { get; set; } = "MOCK"; // MOMO, VNPAY, MOCK
    }
}
