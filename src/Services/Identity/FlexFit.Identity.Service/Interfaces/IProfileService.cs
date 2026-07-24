using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Service.DTOs;

namespace FlexFit.Identity.Service.Interfaces;

public interface IProfileService
{
    Task<MemberProfileDto> GetByUserIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<MemberProfileDto> UpdateAsync(
        Guid userId,
        string fullName,
        string? phoneNumber,
        DateOnly? dateOfBirth,
        string? avatarUrl,
        string? gender,
        decimal? heightCm,
        decimal? weightKg,
        string? fitnessGoal,
        string? activityLevel,
        string? preferredWorkoutTime,
        string? bio,
        CancellationToken cancellationToken = default);
}
