using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class MemberProfile
{
    public Guid MemberProfileId { get; set; }

    public Guid UserId { get; set; }

    public string? Gender { get; set; }

    public DateOnly? DateOfBirth { get; set; }

    public decimal? HeightCm { get; set; }

    public decimal? WeightKg { get; set; }

    public string? FitnessGoal { get; set; }

    public string? ActivityLevel { get; set; }

    public string? PreferredWorkoutTime { get; set; }

    public string? Bio { get; set; }

    public virtual User User { get; set; } = null!;
}
