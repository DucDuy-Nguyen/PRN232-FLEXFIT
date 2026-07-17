using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Application.Abstractions;

/// <summary>
/// Repository abstraction for MemberProfile.
/// Defined in Application; implemented in Infrastructure.
/// </summary>
public interface IMemberProfileRepository
{
    Task<MemberProfile?> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);
    Task AddAsync(MemberProfile profile, CancellationToken cancellationToken = default);
    Task UpdateAsync(MemberProfile profile, CancellationToken cancellationToken = default);
}
