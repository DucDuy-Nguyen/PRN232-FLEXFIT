using System;

namespace FlexFit.Payment.Service.DTOs.Responses
{
    public class PaymentHistoryDto
    {
        public Guid PaymentId { get; set; }
        public Guid UserId { get; set; }
        public string? UserFullName { get; set; }
        public string? UserEmail { get; set; }
        public Guid PackageId { get; set; }
        public string? PackageName { get; set; }
        public decimal Amount { get; set; }
        public string? PaymentMethod { get; set; }
        public string? ProviderTransactionCode { get; set; }
        public string Status { get; set; } = null!;
        public DateTime? PaidAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
