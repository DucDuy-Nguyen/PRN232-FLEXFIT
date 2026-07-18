using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Identity.API.Services.Interfaces;

public interface IRefreshTokenCacheService
{
    Task<RefreshTokenResult> CreateAsync(Guid userId, CancellationToken cancellationToken = default);
    Task<CachedRefreshTokenInfo> ValidateAsync(string rawToken, CancellationToken cancellationToken = default);
    Task<RefreshTokenResult> RotateAsync(string oldRawToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string tokenId, CancellationToken cancellationToken = default);
    Task RevokeFamilyAsync(string familyId, CancellationToken cancellationToken = default);
    Task RevokeUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default);
}

public sealed record RefreshTokenResult(
    string RawToken,
    string TokenId,
    string FamilyId,
    DateTimeOffset ExpiresAt);

public sealed record CachedRefreshTokenInfo(
    string TokenId,
    string FamilyId,
    Guid UserId,
    DateTimeOffset ExpiresAt,
    bool IsRevoked);
