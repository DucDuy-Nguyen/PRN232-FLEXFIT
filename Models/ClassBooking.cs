using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class ClassBooking
{
    public Guid BookingId { get; set; }

    public Guid UserId { get; set; }

    public Guid ClassId { get; set; }

    public string BookingCode { get; set; } = null!;

    public int CreditUsed { get; set; }

    public string? QrToken { get; set; }

    public DateTime? QrExpiresAt { get; set; }

    public Guid? CheckedInBy { get; set; }

    public string CheckInStatus { get; set; } = null!;

    public string Status { get; set; } = null!;

    public DateTime BookedAt { get; set; }

    public DateTime? CancelledAt { get; set; }

    public int RefundCredit { get; set; }

    public DateTime? CheckInTime { get; set; }

    public virtual ICollection<CheckInLog> CheckInLogs { get; set; } = new List<CheckInLog>();

    public virtual User? CheckedInByNavigation { get; set; }

    public virtual Class Class { get; set; } = null!;

    public virtual User User { get; set; } = null!;

    public virtual ICollection<UserWorkoutHistory> UserWorkoutHistories { get; set; } = new List<UserWorkoutHistory>();
}
