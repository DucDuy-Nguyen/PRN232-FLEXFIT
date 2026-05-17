using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class UserWorkoutHistory
{
    public Guid WorkoutHistoryId { get; set; }

    public Guid UserId { get; set; }

    public Guid? GymBookingId { get; set; }

    public Guid? ClassBookingId { get; set; }

    public int? CaloriesBurned { get; set; }

    public int? WorkoutDurationMinutes { get; set; }

    public DateTime CreatedAt { get; set; }

    public virtual ClassBooking? ClassBooking { get; set; }

    public virtual GymBooking? GymBooking { get; set; }

    public virtual User User { get; set; } = null!;
}
