using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class CheckInLog
{
    public Guid CheckInLogId { get; set; }

    public Guid UserId { get; set; }

    public Guid? GymBookingId { get; set; }

    public Guid? ClassBookingId { get; set; }

    public Guid ScannedBy { get; set; }

    public string Status { get; set; } = null!;

    public string? Message { get; set; }

    public DateTime ScannedAt { get; set; }

    public virtual ClassBooking? ClassBooking { get; set; }

    public virtual GymBooking? GymBooking { get; set; }

    public virtual User ScannedByNavigation { get; set; } = null!;

    public virtual User User { get; set; } = null!;
}
