using System;

namespace FlexFit.Payment.API.Contracts.Responses.Payment
{
    public class PaymentCallbackRequest
    {
        public Guid PaymentId { get; set; }
        public string? ProviderTransactionCode { get; set; }
        public string Status { get; set; } = "Success"; // Success, Failed
        public string? Message { get; set; }
    }
}
