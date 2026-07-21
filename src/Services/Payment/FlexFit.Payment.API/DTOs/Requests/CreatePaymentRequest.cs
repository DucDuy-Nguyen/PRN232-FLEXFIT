using System;

namespace FlexFit.Payment.API.DTOs.Requests
{
    public class CreatePaymentRequest
    {
        public Guid PackageId { get; set; }
        public string PaymentMethod { get; set; } = "MOCK"; // PAYOS, MOMO, VNPAY, MOCK
    }
}


