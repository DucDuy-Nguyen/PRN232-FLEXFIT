using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Application.Abstractions;

/// <summary>
/// Repository abstraction for User aggregate.
/// Defined in Application layer; implemented in Infrastructure using EF Core + IdentityDbContext.
///
/// All cross-domain navigation properties (Gyms, BranchStaffs, etc.) are excluded.
/// Only Identity-scoped data is accessible through this repository.
/// </summary>
public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<User?> GetByGoogleSubjectAsync(string googleSubject, CancellationToken cancellationToken = default);
    Task<bool> ExistsByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task AddAsync(User user, CancellationToken cancellationToken = default);
    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
    Task<IEnumerable<User>> GetAllAsync(CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns the total count of users with the Admin role.
    /// Used to enforce the "last Admin" safety rule before revoking Admin access.
    /// </summary>
    Task<int> CountAdminsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// DB-side paginated query with optional filters and sorting.
    /// Avoids loading all users into memory for large datasets.
    /// </summary>
    Task<(IReadOnlyList<User> Items, int TotalCount)> GetPagedAsync(
        int page,
        int pageSize,
        string? search,
        bool? isActive,
        bool? isEmailVerified,
        string? roleName,
        string sortBy,
        bool ascending,
        CancellationToken cancellationToken = default);
}
