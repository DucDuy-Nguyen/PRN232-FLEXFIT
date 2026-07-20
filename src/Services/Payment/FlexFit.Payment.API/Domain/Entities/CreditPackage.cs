using System;
using System.Collections.Generic;

namespace FlexFit.Payment.API.Domain.Entities
{
    public class CreditPackage
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
        public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();
    }
}
