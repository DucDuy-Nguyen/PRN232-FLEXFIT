using System;

namespace FlexFit.Identity.API.Models.DTOs;

public sealed record MemberProfileDto(
    Guid UserId,
    string FullName,
    string Email,
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
