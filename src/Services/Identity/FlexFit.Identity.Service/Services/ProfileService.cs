using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FlexFit.Identity.Service.Interfaces;
using FlexFit.Identity.Service.DTOs;
using FlexFit.Identity.Repository.Entities;
using FlexFit.Identity.Repository.Repositories.Interfaces;

namespace FlexFit.Identity.Service.Services;

public sealed class ProfileService : IProfileService
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProfileService> _logger;

    public ProfileService(
        IUserRepository userRepository,
        IMemberProfileRepository profileRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProfileService> logger)
    {
        _userRepository = userRepository;
        _profileRepository = profileRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<MemberProfileDto> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} not found.");
        }

        var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
        if (profile == null)
        {
            profile = MemberProfile.Create(userId);
            await _profileRepository.AddAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }

        return new MemberProfileDto(
            user.UserId,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth,
            user.AvatarUrl,
            profile.Gender,
            profile.HeightCm,
            profile.WeightKg,
            profile.FitnessGoal,
            profile.ActivityLevel,
            profile.PreferredWorkoutTime,
            profile.Bio
        );
    }

    public async Task<MemberProfileDto> UpdateAsync(
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
        CancellationToken cancellationToken = default)
    {
        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
            if (user == null)
            {
                throw new KeyNotFoundException($"User with ID {userId} not found.");
            }

            user.UpdateProfile(fullName, phoneNumber, dateOfBirth, avatarUrl);
            await _userRepository.UpdateAsync(user, cancellationToken);

            var profile = await _profileRepository.GetByUserIdAsync(userId, cancellationToken);
            if (profile == null)
            {
                profile = MemberProfile.Create(userId);
                profile.Update(gender, heightCm, weightKg, fitnessGoal, activityLevel, preferredWorkoutTime, bio);
                await _profileRepository.AddAsync(profile, cancellationToken);
            }
            else
            {
                profile.Update(gender, heightCm, weightKg, fitnessGoal, activityLevel, preferredWorkoutTime, bio);
                await _profileRepository.UpdateAsync(profile, cancellationToken);
            }

            _logger.LogInformation("Profile successfully updated for user {UserId}", userId);

            return new MemberProfileDto(
                user.UserId,
                user.FullName,
                user.Email,
                user.PhoneNumber,
                user.DateOfBirth,
                user.AvatarUrl,
                profile.Gender,
                profile.HeightCm,
                profile.WeightKg,
                profile.FitnessGoal,
                profile.ActivityLevel,
                profile.PreferredWorkoutTime,
                profile.Bio
            );
        }, cancellationToken);
    }
}
