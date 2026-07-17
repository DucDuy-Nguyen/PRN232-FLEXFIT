using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using FlexFit.Caching;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Exceptions;

namespace FlexFit.Identity.Infrastructure.Security;

public sealed class RedisRefreshTokenCacheService : IRefreshTokenCacheService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ICacheService _cache;
    private readonly IDistributedLockService _lockService;
    private readonly RefreshTokenOptions _options;
    private readonly ILogger<RedisRefreshTokenCacheService> _logger;

    public RedisRefreshTokenCacheService(
        IConnectionMultiplexer connectionMultiplexer,
        ICacheService cache,
        IDistributedLockService lockService,
        IOptions<RefreshTokenOptions> options,
        ILogger<RedisRefreshTokenCacheService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RefreshTokenResult> CreateAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var tokenId = Guid.NewGuid().ToString("N");
        var familyId = Guid.NewGuid().ToString("N");
        
        return await CreateTokenInternalAsync(tokenId, familyId, userId, cancellationToken);
    }

    public async Task<CachedRefreshTokenInfo> ValidateAsync(string rawToken, CancellationToken cancellationToken = default)
    {
        var (tokenId, secret) = ParseRawToken(rawToken);
        var tokenKey = RedisKeys.RefreshToken(tokenId);

        var cached = await _cache.GetAsync<CachedRefreshToken>(tokenKey, cancellationToken);
        if (cached == null)
        {
            _logger.LogWarning("Refresh token {TokenId} not found in cache", tokenId);
            throw new InvalidRefreshTokenException();
        }

        // Verify secret using cryptographically secure fixed-time comparison
        var computedHash = ComputeHash(secret);
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(cached.TokenHash),
            Encoding.UTF8.GetBytes(computedHash));

        if (!isValid)
        {
            _logger.LogWarning("Refresh token {TokenId} signature verification failed", tokenId);
            throw new InvalidRefreshTokenException();
        }

        // Expiry check
        if (DateTimeOffset.UtcNow > cached.ExpiresAt)
        {
            _logger.LogWarning("Refresh token {TokenId} is expired", tokenId);
            throw new InvalidRefreshTokenException();
        }

        // Check if already revoked or replaced (REUSE ATTACK DETECTION)
        if (cached.RevokedAt.HasValue || !string.IsNullOrEmpty(cached.ReplacedByTokenId))
        {
            _logger.LogCritical("REUSE ATTACK DETECTED: Rotated/revoked refresh token {TokenId} was replayed! Revoking all sessions in family {FamilyId}",
                tokenId, cached.TokenFamilyId);

            // Revoke the entire family
            await RevokeFamilyAsync(cached.TokenFamilyId, cancellationToken);
            throw new RefreshTokenReuseException(cached.TokenFamilyId);
        }

        return new CachedRefreshTokenInfo(
            cached.TokenId,
            cached.TokenFamilyId,
            cached.UserId,
            cached.ExpiresAt,
            cached.RevokedAt.HasValue);
    }

    public async Task<RefreshTokenResult> RotateAsync(string oldRawToken, CancellationToken cancellationToken = default)
    {
        var (oldTokenId, _) = ParseRawToken(oldRawToken);
        var oldTokenKey = RedisKeys.RefreshToken(oldTokenId);

        // Fetch old token metadata first to identify the family
        var oldCached = await _cache.GetAsync<CachedRefreshToken>(oldTokenKey, cancellationToken);
        if (oldCached == null)
        {
            throw new InvalidRefreshTokenException();
        }

        var familyId = oldCached.TokenFamilyId;
        var lockName = $"refresh-family:{familyId}";

        // Lock the entire token family to prevent concurrent rotation race conditions
        await using var familyLock = await _lockService.TryAcquireAsync(lockName, TimeSpan.FromSeconds(15), cancellationToken);
        if (familyLock == null)
        {
            _logger.LogWarning("Failed to acquire rotation lock for family {FamilyId}", familyId);
            throw new InvalidRefreshTokenException();
        }

        // Perform validation again inside the lock
        var validatedInfo = await ValidateAsync(oldRawToken, cancellationToken);

        // Generate the new token
        var newTokenId = Guid.NewGuid().ToString("N");
        var result = await CreateTokenInternalAsync(newTokenId, familyId, validatedInfo.UserId, cancellationToken);

        // Revoke the old token and mark replacement
        var revokedOldToken = oldCached with
        {
            RevokedAt = DateTimeOffset.UtcNow,
            ReplacedByTokenId = newTokenId
        };

        var remainingTtl = oldCached.ExpiresAt - DateTimeOffset.UtcNow;
        if (remainingTtl > TimeSpan.Zero)
        {
            await _cache.SetAsync(oldTokenKey, revokedOldToken, remainingTtl, cancellationToken);
        }

        _logger.LogInformation("Successfully rotated refresh token {OldTokenId} to {NewTokenId} in family {FamilyId}", 
            oldTokenId, newTokenId, familyId);

        return result;
    }

    public async Task RevokeAsync(string tokenId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(tokenId)) return;

        var tokenKey = RedisKeys.RefreshToken(tokenId);
        var cached = await _cache.GetAsync<CachedRefreshToken>(tokenKey, cancellationToken);
        if (cached == null) return;

        var updated = cached with { RevokedAt = DateTimeOffset.UtcNow };
        var remainingTtl = cached.ExpiresAt - DateTimeOffset.UtcNow;

        if (remainingTtl > TimeSpan.Zero)
        {
            await _cache.SetAsync(tokenKey, updated, remainingTtl, cancellationToken);
        }
    }

    public async Task RevokeFamilyAsync(string familyId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(familyId)) return;

        var db = _connectionMultiplexer.GetDatabase();
        var familyKey = RedisKeys.RefreshTokenFamily(familyId);

        // Read all token IDs in the family Set
        var tokenIds = await db.SetMembersAsync(familyKey);
        
        foreach (var tokenIdVal in tokenIds)
        {
            var tokenIdStr = tokenIdVal.ToString();
            await RevokeAsync(tokenIdStr, cancellationToken);
        }

        // Delete the family index key
        await db.KeyDeleteAsync(familyKey);
    }

    public async Task RevokeUserSessionsAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var db = _connectionMultiplexer.GetDatabase();
        var userSessionsKey = $"flexfit:identity:user-sessions:{userId}";

        // Read all token IDs belonging to this user
        var tokenIds = await db.SetMembersAsync(userSessionsKey);

        foreach (var tokenIdVal in tokenIds)
        {
            var tokenIdStr = tokenIdVal.ToString();
            var tokenKey = RedisKeys.RefreshToken(tokenIdStr);

            var cached = await _cache.GetAsync<CachedRefreshToken>(tokenKey, cancellationToken);
            if (cached != null)
            {
                // Revoke the entire family to clear concurrent rotated sets
                await RevokeFamilyAsync(cached.TokenFamilyId, cancellationToken);
            }
        }

        // Clear the user's active session set
        await db.KeyDeleteAsync(userSessionsKey);
        _logger.LogInformation("Revoked all refresh sessions for user {UserId}", userId);
    }

    private async Task<RefreshTokenResult> CreateTokenInternalAsync(
        string tokenId, 
        string familyId, 
        Guid userId, 
        CancellationToken cancellationToken)
    {
        // 1. Generate secure secret (32 bytes entropy)
        var secretBytes = RandomNumberGenerator.GetBytes(32);
        var secret = Convert.ToBase64String(secretBytes)
            .Replace("+", "-").Replace("/", "_").Replace("=", ""); // Base64Url safe conversion

        var hash = ComputeHash(secret);
        var expiresAt = DateTimeOffset.UtcNow.AddDays(_options.ExpiryInDays);

        var cachedToken = new CachedRefreshToken(
            TokenId: tokenId,
            TokenFamilyId: familyId,
            UserId: userId,
            TokenHash: hash,
            CreatedAt: DateTimeOffset.UtcNow,
            ExpiresAt: expiresAt,
            RevokedAt: null,
            ReplacedByTokenId: null,
            DeviceId: null);

        // 2. Save metadata to Redis
        var tokenKey = RedisKeys.RefreshToken(tokenId);
        await _cache.SetAsync(tokenKey, cachedToken, TimeSpan.FromDays(_options.ExpiryInDays), cancellationToken);

        // 3. Add to family index set
        var db = _connectionMultiplexer.GetDatabase();
        var familyKey = RedisKeys.RefreshTokenFamily(familyId);
        await db.SetAddAsync(familyKey, tokenId);
        await db.KeyExpireAsync(familyKey, TimeSpan.FromDays(_options.ExpiryInDays));

        // 4. Add to user session index set
        var userSessionsKey = $"flexfit:identity:user-sessions:{userId}";
        await db.SetAddAsync(userSessionsKey, tokenId);
        await db.KeyExpireAsync(userSessionsKey, TimeSpan.FromDays(_options.ExpiryInDays));

        // Format rawToken: {tokenId}.{secret}
        var rawToken = $"{tokenId}.{secret}";

        return new RefreshTokenResult(rawToken, tokenId, familyId, expiresAt);
    }

    private static (string TokenId, string Secret) ParseRawToken(string rawToken)
    {
        if (string.IsNullOrWhiteSpace(rawToken))
        {
            throw new InvalidRefreshTokenException();
        }

        var parts = rawToken.Split('.');
        if (parts.Length != 2)
        {
            throw new InvalidRefreshTokenException();
        }

        return (parts[0], parts[1]);
    }

    private static string ComputeHash(string input)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToBase64String(bytes);
    }
}
