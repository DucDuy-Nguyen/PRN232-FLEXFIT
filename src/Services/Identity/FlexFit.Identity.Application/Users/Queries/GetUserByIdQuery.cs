using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Application.Users.Queries;

public sealed record GetUserByIdQuery(Guid UserId) : IRequest<UserDto>;

public sealed class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly IUserRepository _userRepository;

    public GetUserByIdQueryHandler(IUserRepository userRepository)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
    }

    public async Task<UserDto> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {request.UserId} not found.");
        }

        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();

        return new UserDto(
            user.UserId,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsEmailVerified,
            user.IsActive,
            user.AvatarUrl,
            roles,
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt);
    }
}
