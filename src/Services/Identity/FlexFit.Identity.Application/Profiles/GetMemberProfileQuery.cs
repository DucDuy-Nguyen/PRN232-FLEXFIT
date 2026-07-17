using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Application.Profiles;

public sealed record GetMemberProfileQuery(Guid UserId) : IRequest<MemberProfileDto>;

public sealed class GetMemberProfileQueryHandler : IRequestHandler<GetMemberProfileQuery, MemberProfileDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IMemberProfileRepository _profileRepository;

    public GetMemberProfileQueryHandler(IUserRepository userRepository, IMemberProfileRepository profileRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
    }

    public async Task<MemberProfileDto> Handle(GetMemberProfileQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException($"User with ID {request.UserId} not found.");
        }

        var profile = await _profileRepository.GetByUserIdAsync(request.UserId, cancellationToken);
        
        return new MemberProfileDto(
            user.UserId,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.DateOfBirth,
            user.AvatarUrl,
            profile?.Gender,
            profile?.HeightCm,
            profile?.WeightKg,
            profile?.FitnessGoal,
            profile?.ActivityLevel,
            profile?.PreferredWorkoutTime,
            profile?.Bio);
    }
}
