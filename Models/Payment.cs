using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class Payment
{
    public Guid PaymentId { get; set; }

    public Guid UserId { get; set; }

    public Guid PackageId { get; set; }

    public decimal Amount { get; set; }

    public string? PaymentMethod { get; set; }

    public string? ProviderTransactionCode { get; set; }

    public string Status { get; set; } = null!;

    public DateTime? PaidAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual CreditPackage Package { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
