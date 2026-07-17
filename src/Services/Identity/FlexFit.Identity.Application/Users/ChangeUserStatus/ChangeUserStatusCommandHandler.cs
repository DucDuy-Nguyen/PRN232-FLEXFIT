using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Application.Users.ChangeUserStatus;

public sealed class ChangeUserStatusCommandHandler : IRequestHandler<ChangeUserStatusCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IRedisEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangeUserStatusCommandHandler> _logger;

    public ChangeUserStatusCommandHandler(
        IUserRepository userRepository,
        IRefreshTokenCacheService refreshTokenCache,
        ITokenBlacklistService tokenBlacklistService,
        IRedisEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<ChangeUserStatusCommandHandler> _logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _refreshTokenCache = refreshTokenCache ?? throw new ArgumentNullException(nameof(refreshTokenCache));
        _tokenBlacklistService = tokenBlacklistService ?? throw new ArgumentNullException(nameof(tokenBlacklistService));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        this._logger = _logger ?? throw new ArgumentNullException(nameof(_logger));
    }

    public async Task<Unit> Handle(ChangeUserStatusCommand request, CancellationToken cancellationToken)
    {
        // 1. Find user
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {request.TargetUserId} not found.");
        }

        // 2. Prevent self-deactivation
        if (request.TargetUserId == request.ActorUserId && !request.IsActive)
        {
            throw new InvalidOperationException("You cannot deactivate or lock your own account.");
        }

        var oldIsActive = user.IsActive;
        if (oldIsActive == request.IsActive)
        {
            // Nothing changed
            return Unit.Value;
        }

        // 3. Set new status
        user.SetActive(request.IsActive);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} status changed from {OldActive} to {NewActive} by {ActorId}.",
            request.TargetUserId, oldIsActive, request.IsActive, request.ActorUserId);

        // 4. Revoke active sessions if user is being deactivated
        if (!request.IsActive)
        {
            try
            {
                await _refreshTokenCache.RevokeUserSessionsAsync(request.TargetUserId, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to revoke refresh tokens for user {UserId} after deactivation.", request.TargetUserId);
            }
        }

        // 5. Publish event (dual-write risk acknowledged; Outbox pattern deferred)
        try
        {
            await _eventPublisher.PublishAsync(RedisStreams.IdentityEvents, new UserStatusChangedEvent
            {
                UserId = user.UserId,
                Email = user.Email,
                OldIsActive = oldIsActive,
                NewIsActive = user.IsActive,
                ChangedBy = request.ActorUserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserStatusChangedEvent for user {UserId}.", user.UserId);
        }

        return Unit.Value;
    }
}
