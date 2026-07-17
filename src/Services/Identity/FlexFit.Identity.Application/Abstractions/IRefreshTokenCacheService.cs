using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Identity.Application.Abstractions;

public interface IRefreshTokenCacheService
{
    /// <summary>
    /// Generates a new refresh token, registers it in Redis, and returns the raw token string (tokenId.secret) for the client.
    /// </summary>
    Task<RefreshTokenResult> CreateAsync(Guid userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a raw refresh token (tokenId.secret).
    /// Returns the token metadata if valid.
    /// Throws InvalidRefreshTokenException or RefreshTokenReuseException.
    /// </summary>
    Task<CachedRefreshTokenInfo> ValidateAsync(string rawToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rotates the refresh token. Validates the old token, revokes it, and issues a new one in the same family.
    /// Protects against race conditions using a distributed lock.
    /// </summary>
    Task<RefreshTokenResult> RotateAsync(string oldRawToken, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes a single token session by token ID.
    /// </summary>
    Task RevokeAsync(string tokenId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active sessions in a family (used when reuse attack is detected).
    /// </summary>
    Task RevokeFamilyAsync(string familyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Revokes all active sessions belonging to the given user (used upon password changes/resets).
    /// </summary>
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
