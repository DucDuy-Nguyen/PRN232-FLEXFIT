using System;

namespace FlexFit.Payment.Service.DTOs.Responses
{
    public class PaymentCallbackRequest
    {
        public Guid PaymentId { get; set; }
        public string? ProviderTransactionCode { get; set; }
        public string Status { get; set; } = "Success"; // Success, Failed
        public string? Message { get; set; }
    }
}
