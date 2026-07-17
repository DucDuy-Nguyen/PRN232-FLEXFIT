using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Infrastructure.Persistence.Repositories;

public sealed class UserRepository : IUserRepository
{
    private readonly IdentityDbContext _context;

    public UserRepository(IdentityDbContext context)
    {
        _context = context ?? throw new ArgumentNullException(nameof(context));
    }

    public Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.MemberProfile)
            .FirstOrDefaultAsync(u => u.UserId == userId, cancellationToken);
    }

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();

        return _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.MemberProfile)
            .FirstOrDefaultAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email cannot be null or whitespace.", nameof(email));
        }

        var normalizedEmail = email.Trim().ToLowerInvariant();
        return _context.Users
            .AnyAsync(u => u.Email.ToLower() == normalizedEmail, cancellationToken);
    }

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        await _context.Users.AddAsync(user, cancellationToken);
    }

    public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        _context.Users.Update(user);
        return Task.CompletedTask;
    }

    public async Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _context.Users
            .AsNoTracking()
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync(cancellationToken);
    }

    public async Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var user = await _context.Users.FindAsync(new object[] { userId }, cancellationToken);
        if (user != null)
        {
            _context.Users.Remove(user);
        }
    }

    public Task<User?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(googleSubject))
        {
            return Task.FromResult<User?>(null);
        }

        return _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.MemberProfile)
            .FirstOrDefaultAsync(u => u.GoogleSubject == googleSubject, cancellationToken);
    }

    public Task<int> CountAdminsAsync(CancellationToken cancellationToken = default)
    {
        return _context.UserRoles
            .CountAsync(ur => ur.Role.RoleName == "Admin", cancellationToken);
    }

    public async Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        bool? isEmailVerified,
        string? roleName,
        string sortBy,
        bool ascending,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Users
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .Include(u => u.MemberProfile)
            .AsNoTracking();

        // Apply filters
        if (!string.IsNullOrWhiteSpace(search))
        {
            var cleanSearch = search.Trim().ToLower();
            query = query.Where(u => u.FullName.ToLower().Contains(cleanSearch) || 
                                     u.Email.ToLower().Contains(cleanSearch) ||
                                     (u.PhoneNumber != null && u.PhoneNumber.Contains(cleanSearch)));
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        if (isEmailVerified.HasValue)
        {
            query = query.Where(u => u.IsEmailVerified == isEmailVerified.Value);
        }

        if (!string.IsNullOrWhiteSpace(roleName))
        {
            query = query.Where(u => u.UserRoles.Any(ur => ur.Role.RoleName == roleName));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // Sorting
        query = sortBy.ToLower() switch
        {
            "fullname" => ascending ? query.OrderBy(u => u.FullName) : query.OrderByDescending(u => u.FullName),
            "email" => ascending ? query.OrderBy(u => u.Email) : query.OrderByDescending(u => u.Email),
            "lastloginat" => ascending ? query.OrderBy(u => u.LastLoginAt) : query.OrderByDescending(u => u.LastLoginAt),
            "updatedat" => ascending ? query.OrderBy(u => u.UpdatedAt) : query.OrderByDescending(u => u.UpdatedAt),
            _ => ascending ? query.OrderBy(u => u.CreatedAt) : query.OrderByDescending(u => u.CreatedAt)
        };

        // Paginate
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }
}
