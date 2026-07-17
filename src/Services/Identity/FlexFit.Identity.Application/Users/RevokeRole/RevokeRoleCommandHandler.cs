using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Application.Users.RevokeRole;

public sealed class RevokeRoleCommandHandler : IRequestHandler<RevokeRoleCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly IRedisEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RevokeRoleCommandHandler> _logger;

    public RevokeRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenCacheService refreshTokenCache,
        IRedisEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<RevokeRoleCommandHandler> logger)
    {
        _userRepository   = userRepository   ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository   = roleRepository   ?? throw new ArgumentNullException(nameof(roleRepository));
        _refreshTokenCache = refreshTokenCache ?? throw new ArgumentNullException(nameof(refreshTokenCache));
        _eventPublisher   = eventPublisher   ?? throw new ArgumentNullException(nameof(eventPublisher));
        _unitOfWork       = unitOfWork       ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> Handle(RevokeRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Resolve role
        var role = await _roleRepository.GetByNameAsync(request.RoleName, cancellationToken);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role '{request.RoleName}' does not exist.");
        }

        // 2. Find existing assignment
        var userRole = await _roleRepository.GetUserRoleAsync(request.TargetUserId, role.RoleId, cancellationToken);
        if (userRole == null)
        {
            throw new KeyNotFoundException(
                $"User {request.TargetUserId} does not have the '{request.RoleName}' role.");
        }

        // 3. Safety rule: prevent removing the last Admin in the system
        if (string.Equals(request.RoleName, "Admin", StringComparison.OrdinalIgnoreCase))
        {
            var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException(
                    "Cannot revoke the Admin role from the last administrator in the system.");
            }
        }

        // 4. Remove the role assignment
        await _roleRepository.RemoveUserRoleAsync(userRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role '{Role}' revoked from user {UserId} by {ActorId}.",
            request.RoleName, request.TargetUserId, request.ActorUserId);

        // 5. Revoke all refresh tokens for the target user — their permissions changed
        try
        {
            await _refreshTokenCache.RevokeUserSessionsAsync(request.TargetUserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke sessions for user {UserId} after role revocation.", request.TargetUserId);
            // Non-blocking: refresh tokens will expire naturally; access tokens are unaffected
        }

        // 6. Publish integration event
        try
        {
            await _eventPublisher.PublishAsync(RedisStreams.IdentityEvents, new UserRoleChangedEvent
            {
                UserId    = request.TargetUserId,
                RoleName  = role.RoleName,
                Action    = "Revoked",
                ChangedBy = request.ActorUserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserRoleChangedEvent for user {UserId}.", request.TargetUserId);
        }

        return Unit.Value;
    }
}
