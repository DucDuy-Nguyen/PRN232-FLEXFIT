using System;

namespace Flexfit.DTOs.Payment
{
    public class CreditPackageResponse
    {
        public Guid PackageId { get; set; }
        public string PackageName { get; set; } = null!;
        public int CreditAmount { get; set; }
        public int BonusCredit { get; set; }
        public decimal Price { get; set; }
        public string? Description { get; set; }
        public bool IsPopular { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
