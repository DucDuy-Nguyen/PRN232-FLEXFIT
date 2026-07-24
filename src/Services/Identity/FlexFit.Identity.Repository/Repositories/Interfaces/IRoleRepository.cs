using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Repository.Entities;
using FlexFit.Identity.Repository.Repositories.Interfaces;

namespace FlexFit.Identity.Repository.Repositories.Interfaces;

public interface IRoleRepository
{
    Task<Role?> GetByNameAsync(string roleName, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Role>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<UserRole?> GetUserRoleAsync(Guid userId, Guid roleId, CancellationToken cancellationToken = default);
    Task AddUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
    Task RemoveUserRoleAsync(UserRole userRole, CancellationToken cancellationToken = default);
}
