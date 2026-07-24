using System;

namespace FlexFit.Identity.Service.DTOs.Contracts.Profiles;

public sealed record UpdateProfileRequest(
    string FullName,
    string? PhoneNumber,
    DateOnly? DateOfBirth,
    string? AvatarUrl,
    string? Gender,
    decimal? HeightCm,
    decimal? WeightKg,
    string? FitnessGoal,
    string? ActivityLevel,
    string? PreferredWorkoutTime,
    string? Bio);
