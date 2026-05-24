using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class Review
{
    public Guid ReviewId { get; set; }

    public Guid UserId { get; set; }

    public Guid? GymId { get; set; }

    public Guid? ClassId { get; set; }

    public int Rating { get; set; }

    public string? Comment { get; set; }

    public DateTime CreatedAt { get; set; }

    public Guid? ClassBookingId { get; set; }

    public Guid? GymBookingId { get; set; }

    public virtual Class? Class { get; set; }

    public virtual Gym? Gym { get; set; }

    public virtual ClassBooking? ClassBooking { get; set; }

    public virtual GymBooking? GymBooking { get; set; }

    public virtual User User { get; set; } = null!;
}
