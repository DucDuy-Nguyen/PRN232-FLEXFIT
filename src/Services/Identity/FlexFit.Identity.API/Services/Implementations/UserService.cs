using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using FlexFit.Identity.API.Services.Interfaces;
using FlexFit.Identity.API.Services.Interfaces;
using FlexFit.Identity.API.Models.DTOs;
using FlexFit.Identity.API.Models.Entities;
using FlexFit.Identity.API.Models.Exceptions;
using FlexFit.RedisEventBus;
using FlexFit.Contracts;
using FlexFit.Identity.API.Data.Repositories.Interfaces;

namespace FlexFit.Identity.API.Services.Implementations;

public sealed class UserService : IUserService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly ITokenBlacklistService _tokenBlacklistService;
    private readonly IRedisEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<UserService> _logger;

    public UserService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IRefreshTokenCacheService refreshTokenCache,
        ITokenBlacklistService tokenBlacklistService,
        IRedisEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<UserService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _refreshTokenCache = refreshTokenCache;
        _tokenBlacklistService = tokenBlacklistService;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task AssignRoleAsync(Guid targetUserId, string roleName, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {targetUserId} was not found.");
        }

        var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role '{roleName}' was not found in the database.");
        }

        var existingUserRole = await _roleRepository.GetUserRoleAsync(targetUserId, role.RoleId, cancellationToken);
        if (existingUserRole != null)
        {
            throw new InvalidOperationException($"User already has the role '{roleName}'.");
        }

        var newUserRole = UserRole.Create(targetUserId, role.RoleId);
        await _roleRepository.AddUserRoleAsync(newUserRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Role '{RoleName}' successfully assigned to user {UserId} by Admin {AdminId}", 
            roleName, targetUserId, actorUserId);

        await _eventPublisher.PublishAsync("identity-events", new UserRoleChangedEvent
        {
            UserId = targetUserId,
            RoleName = roleName,
            Action = "Assigned",
            ChangedBy = actorUserId
        });
    }

    public async Task RevokeRoleAsync(Guid targetUserId, string roleName, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        var role = await _roleRepository.GetByNameAsync(roleName, cancellationToken);
        if (role == null)
        {
            throw new KeyNotFoundException($"Role '{roleName}' was not found.");
        }

        var userRole = await _roleRepository.GetUserRoleAsync(targetUserId, role.RoleId, cancellationToken);
        if (userRole == null)
        {
            throw new KeyNotFoundException($"User role assignment for '{roleName}' not found.");
        }

        if (roleName.Equals("Admin", StringComparison.OrdinalIgnoreCase))
        {
            var adminCount = await _userRepository.CountAdminsAsync(cancellationToken);
            if (adminCount <= 1)
            {
                throw new InvalidOperationException("Cannot revoke the last remaining Administrator role from the system.");
            }
        }

        await _roleRepository.RemoveUserRoleAsync(userRole, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // Invalidate active sessions to clear roles cache in Jwt Token on next request/refresh
        await _refreshTokenCache.RevokeUserSessionsAsync(targetUserId, cancellationToken);

        _logger.LogInformation("Role '{RoleName}' successfully revoked from user {UserId} by Admin {AdminId}", 
            roleName, targetUserId, actorUserId);

        await _eventPublisher.PublishAsync("identity-events", new UserRoleChangedEvent
        {
            UserId = targetUserId,
            RoleName = roleName,
            Action = "Revoked",
            ChangedBy = actorUserId
        });
    }

    public async Task ChangeUserStatusAsync(Guid targetUserId, bool isActive, Guid actorUserId, CancellationToken cancellationToken = default)
    {
        if (targetUserId == actorUserId)
        {
            throw new InvalidOperationException("Administrator cannot deactivate or lock their own account.");
        }

        var user = await _userRepository.GetByIdAsync(targetUserId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {targetUserId} was not found.");
        }

        var oldIsActive = user.IsActive;
        if (oldIsActive == isActive)
        {
            return;
        }

        user.SetActive(isActive);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        if (!isActive)
        {
            // Revoke active sessions on account lock
            await _refreshTokenCache.RevokeUserSessionsAsync(targetUserId, cancellationToken);
        }

        _logger.LogInformation("User {UserId} status changed from {OldStatus} to {NewStatus} by Admin {AdminId}", 
            targetUserId, oldIsActive ? "Active" : "Inactive", isActive ? "Active" : "Inactive", actorUserId);

        await _eventPublisher.PublishAsync("identity-events", new UserStatusChangedEvent
        {
            UserId = targetUserId,
            Email = user.Email,
            OldIsActive = oldIsActive,
            NewIsActive = isActive,
            ChangedBy = actorUserId
        });
    }

    public async Task<UserDto> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null)
        {
            throw new KeyNotFoundException($"User with ID {userId} was not found.");
        }

        var roles = user.UserRoles
            .Select(ur => ur.Role?.RoleName)
            .Where(r => r != null)
            .Select(r => r!)
            .ToList();

        return new UserDto(
            user.UserId,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsEmailVerified,
            user.IsActive,
            user.AvatarUrl,
            roles.AsReadOnly(),
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt
        );
    }

    public async Task<PaginatedList<UserDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        bool? isEmailVerified,
        string? roleName,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default)
    {
        if (page < 1) page = 1;
        if (pageSize < 1) pageSize = 10;
        if (pageSize > 100) pageSize = 100; // clamp to 100 max

        var ascending = !sortDirection.Equals("desc", StringComparison.OrdinalIgnoreCase);

        var (items, totalCount) = await _userRepository.GetPagedAsync(
            page,
            pageSize,
            search,
            isActive,
            isEmailVerified,
            roleName,
            sortBy,
            ascending,
            cancellationToken
        );

        var dtos = items.Select(user => new UserDto(
            user.UserId,
            user.FullName,
            user.Email,
            user.PhoneNumber,
            user.IsEmailVerified,
            user.IsActive,
            user.AvatarUrl,
            user.UserRoles.Select(ur => ur.Role?.RoleName ?? string.Empty).Where(name => !string.IsNullOrEmpty(name)).ToList().AsReadOnly(),
            user.CreatedAt,
            user.UpdatedAt,
            user.LastLoginAt
        )).ToList();

        return new PaginatedList<UserDto>(dtos, totalCount, page, pageSize);
    }
}
