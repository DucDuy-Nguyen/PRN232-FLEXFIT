using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.API.Models.Entities;
using FlexFit.Identity.API.Data.Repositories.Interfaces;

namespace FlexFit.Identity.API.Data.Repositories.Interfaces;

public interface IMemberProfileRepository
{
    Task<MemberProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(MemberProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(MemberProfile profile, CancellationToken cancellationToken = default);
}
