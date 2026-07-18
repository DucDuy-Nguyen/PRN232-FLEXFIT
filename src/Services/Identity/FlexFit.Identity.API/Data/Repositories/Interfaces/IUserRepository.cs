using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.API.Models.Entities;
using FlexFit.Identity.API.Data.Repositories.Interfaces;

namespace FlexFit.Identity.API.Data.Repositories.Interfaces;

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
    Task<int> CountAdminsAsync(CancellationToken cancellationToken = default);
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
