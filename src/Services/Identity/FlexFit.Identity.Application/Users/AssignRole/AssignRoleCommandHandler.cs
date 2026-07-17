using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Application.Users.AssignRole;

public sealed class AssignRoleCommandHandler : IRequestHandler<AssignRoleCommand, Unit>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRedisEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<AssignRoleCommandHandler> _logger;

    public AssignRoleCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRedisEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<AssignRoleCommandHandler> logger)
    {
        _userRepository  = userRepository  ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository  = roleRepository  ?? throw new ArgumentNullException(nameof(roleRepository));
        _eventPublisher  = eventPublisher  ?? throw new ArgumentNullException(nameof(eventPublisher));
        _unitOfWork      = unitOfWork      ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger          = logger          ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<Unit> Handle(AssignRoleCommand request, CancellationToken cancellationToken)
    {
        // 1. Load target user (with roles)
        var user = await _userRepository.GetByIdAsync(request.TargetUserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {request.TargetUserId} not found.");
        }

        // 2. Resolve role
        var role = await _roleRepository.GetByNameAsync(request.RoleName, cancellationToken);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role '{request.RoleName}' does not exist in system.");
        }

        // 3. Check for duplicate assignment
        var existing = await _roleRepository.GetUserRoleAsync(user.UserId, role.RoleId, cancellationToken);
        if (existing != null)
        {
            throw new InvalidOperationException($"User already has the '{request.RoleName}' role.");
        }

        // 4. Create and persist UserRole
        var userRole = UserRole.Create(user.UserId, role.RoleId);
        await _roleRepository.AddUserRoleAsync(userRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role '{Role}' assigned to user {UserId} by {ActorId}.",
            request.RoleName, request.TargetUserId, request.ActorUserId);

        // 5. Publish event (dual-write risk acknowledged; Outbox pattern deferred)
        try
        {
            await _eventPublisher.PublishAsync(RedisStreams.IdentityEvents, new UserRoleChangedEvent
            {
                UserId    = user.UserId,
                RoleName  = role.RoleName,
                Action    = "Assigned",
                ChangedBy = request.ActorUserId
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserRoleChangedEvent for user {UserId}.", user.UserId);
        }

        return Unit.Value;
    }
}
