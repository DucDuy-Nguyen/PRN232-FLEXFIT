namespace Flexfit.DTOs.Payment
{
    public class PaymentCallbackRequest
    {
        public System.Guid PaymentId { get; set; }
        public string? ProviderTransactionCode { get; set; }
        public string Status { get; set; } = "Success"; // Success, Failed
        public string? Message { get; set; }
    }
}
