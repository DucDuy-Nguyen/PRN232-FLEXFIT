using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.API.Models.DTOs;

namespace FlexFit.Identity.API.Services.Interfaces;

public interface IUserService
{
    Task AssignRoleAsync(
        Guid targetUserId, 
        string roleName, 
        Guid actorUserId, 
        CancellationToken cancellationToken = default);

    Task RevokeRoleAsync(
        Guid targetUserId, 
        string roleName, 
        Guid actorUserId, 
        CancellationToken cancellationToken = default);

    Task ChangeUserStatusAsync(
        Guid targetUserId, 
        bool isActive, 
        Guid actorUserId, 
        CancellationToken cancellationToken = default);

    Task<UserDto> GetByIdAsync(
        Guid userId, 
        CancellationToken cancellationToken = default);

    Task<PaginatedList<UserDto>> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        bool? isEmailVerified,
        string? roleName,
        string sortBy,
        string sortDirection,
        CancellationToken cancellationToken = default);
}
