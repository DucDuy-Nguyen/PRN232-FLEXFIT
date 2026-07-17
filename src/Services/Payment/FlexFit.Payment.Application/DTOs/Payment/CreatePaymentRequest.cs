using System;

namespace FlexFit.Payment.Application.DTOs.Payment
{
    public class CreatePaymentRequest
    {
        public Guid PackageId { get; set; }
        public string PaymentMethod { get; set; } = "MOCK"; // PAYOS, MOMO, VNPAY, MOCK
    }
}
