using System;

namespace FlexFit.Identity.API.Services.Implementations;

public sealed record CachedRefreshToken(
    string TokenId,
    string TokenFamilyId,
    Guid UserId,
    string TokenHash,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt,
    DateTimeOffset? RevokedAt,
    string? ReplacedByTokenId,
    string? DeviceId);
