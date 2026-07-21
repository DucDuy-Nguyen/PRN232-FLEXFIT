using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Repository.Entities;
using FlexFit.Identity.Repository.Repositories.Interfaces;

namespace FlexFit.Identity.Repository.Repositories.Interfaces;

public interface IMemberProfileRepository
{
    Task<MemberProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(MemberProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(MemberProfile profile, CancellationToken cancellationToken = default);
}
