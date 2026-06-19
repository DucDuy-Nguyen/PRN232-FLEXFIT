using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class GymSession
{
    public Guid SessionId { get; set; }

    public Guid BranchId { get; set; }

    public string? SessionName { get; set; }

    public DateTime StartTime { get; set; }

    public DateTime EndTime { get; set; }

    public int Capacity { get; set; }

    public int CreditCost { get; set; }

    public string Status { get; set; } = null!;

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual Branch Branch { get; set; } = null!;

    public virtual ICollection<GymBooking> GymBookings { get; set; } = new List<GymBooking>();
}
