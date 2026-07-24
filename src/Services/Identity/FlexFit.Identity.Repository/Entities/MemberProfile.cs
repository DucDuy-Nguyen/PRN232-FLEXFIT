using System;

namespace FlexFit.Identity.Repository.Entities;

/// <summary>
/// MemberProfile entity - extends User with fitness-specific data.
/// </summary>
public sealed class MemberProfile
{
    public Guid MemberProfileId { get; private set; }   // Guid, default newid()
    public Guid UserId { get; private set; }            // Guid, UNIQUE FK to Users

    public string? Gender { get; private set; }
    public decimal? HeightCm { get; private set; }
    public decimal? WeightKg { get; private set; }
    public string? FitnessGoal { get; private set; }
    public string? ActivityLevel { get; private set; }
    public string? PreferredWorkoutTime { get; private set; }
    public string? Bio { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;

    // EF Core constructor
    private MemberProfile() { }

    public static MemberProfile Create(Guid userId)
    {
        return new MemberProfile
        {
            MemberProfileId = Guid.NewGuid(),
            UserId = userId
        };
    }

    public void Update(
        string? gender,
        decimal? heightCm,
        decimal? weightKg,
        string? fitnessGoal,
        string? activityLevel,
        string? preferredWorkoutTime,
        string? bio)
    {
        Gender = gender;
        HeightCm = heightCm;
        WeightKg = weightKg;
        FitnessGoal = fitnessGoal;
        ActivityLevel = activityLevel;
        PreferredWorkoutTime = preferredWorkoutTime;
        Bio = bio;
    }
}
