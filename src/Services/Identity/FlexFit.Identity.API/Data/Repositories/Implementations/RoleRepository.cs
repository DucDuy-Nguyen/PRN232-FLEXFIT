using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlexFit.Identity.API.Data;
using FlexFit.Identity.API.Services.Interfaces;
using FlexFit.Identity.API.Models.Entities;
using FlexFit.Identity.API.Data.Repositories.Interfaces;

namespace FlexFit.Identity.API.Data.Repositories.Implementations;

public sealed class RoleRepository : IRoleRepository
{
    private readonly IdentityDbContext _context;

    public RoleRepository(IdentityDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(roleName))
        {
            throw new ArgumentException("Role name cannot be null or whitespace.", nameof(roleName));
        }

        return _context.Roles
            .FirstOrDefaultAsync(r => r.RoleName == roleName, cancellationToken);
    }

    public Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default)
    {
        return _context.UserRoles
            .FirstOrDefaultAsync(ur => ur.UserId == userId && ur.RoleId == roleId, cancellationToken);
    }

    public async Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        if (userRole == null) throw new ArgumentNullException(nameof(userRole));
        await _context.UserRoles.AddAsync(userRole, cancellationToken);
    }

    public Task RemoveUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default)
    {
        if (userRole == null) throw new ArgumentNullException(nameof(userRole));
        _context.UserRoles.Remove(userRole);
        return Task.CompletedTask;
    }

    public async Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var items = await _context.Roles.AsNoTracking().ToListAsync(cancellationToken);
        return items.AsReadOnly();
    }
}
