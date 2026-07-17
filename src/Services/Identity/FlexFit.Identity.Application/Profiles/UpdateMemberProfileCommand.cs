using System;
using System.Threading;
using System.Threading.Tasks;
using FluentValidation;
using MediatR;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Application.Profiles;

public sealed record UpdateMemberProfileCommand(
    Guid UserId,
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
    string? Bio) : IRequest<MemberProfileDto>;

public sealed class UpdateMemberProfileCommandValidator : AbstractValidator<UpdateMemberProfileCommand>
{
    public UpdateMemberProfileCommandValidator()
    {
        RuleFor(x => x.FullName)
            .NotEmpty().WithMessage("Full name is required.")
            .MaximumLength(100).WithMessage("Full name cannot exceed 100 characters.");

        RuleFor(x => x.PhoneNumber)
            .MaximumLength(20).WithMessage("Phone number cannot exceed 20 characters.");

        RuleFor(x => x.Gender)
            .MaximumLength(20).WithMessage("Gender cannot exceed 20 characters.");

        RuleFor(x => x.ActivityLevel)
            .MaximumLength(50).WithMessage("Activity level cannot exceed 50 characters.");

        RuleFor(x => x.PreferredWorkoutTime)
            .MaximumLength(50).WithMessage("Preferred workout time cannot exceed 50 characters.");
    }
}

public sealed class UpdateMemberProfileCommandHandler : IRequestHandler<UpdateMemberProfileCommand, MemberProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateMemberProfileCommandHandler(
        IUserRepository userRepository, 
        IMemberProfileRepository profileRepository,
        IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
    }

    public async Task<MemberProfileDto> Handle(UpdateMemberProfileCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch User and update profile fields
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} not found.");
        }

        user.UpdateProfile(request.FullName, request.PhoneNumber, request.DateOfBirth, request.AvatarUrl);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 2. Fetch or create MemberProfile record
        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        if (profile == null)
        {
            profile = MemberProfile.Create(request.UserId);
            profile.Update(
                request.Gender,
                request.HeightCm,
                request.WeightKg,
                request.FitnessGoal,
                request.ActivityLevel,
                request.PreferredWorkoutTime,
                request.Bio);
            await _profileRepository.AddAsync(profile, cancellationToken);
        }
        else
        {
            profile.Update(
                request.Gender,
                request.HeightCm,
                request.WeightKg,
                request.FitnessGoal,
                request.ActivityLevel,
                request.PreferredWorkoutTime,
                request.Bio);
            await _profileRepository.UpdateAsync(profile, cancellationToken);
        }

        // 3. Persist DB changes (TransactionBehavior will wrap this if ending in "Command")
        await _unitOfWork.SaveChangesAsync(cancellationToken);

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
            profile.Bio);
    }
}
