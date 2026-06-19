using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class CreditTransaction
{
    public Guid TransactionId { get; set; }

    public Guid UserId { get; set; }

    public int Amount { get; set; }

    public int BalanceBefore { get; set; }

    public int BalanceAfter { get; set; }

    public string Type { get; set; } = null!;

    public Guid? ReferenceId { get; set; }

    public string? ReferenceType { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual User User { get; set; } = null!;
}
