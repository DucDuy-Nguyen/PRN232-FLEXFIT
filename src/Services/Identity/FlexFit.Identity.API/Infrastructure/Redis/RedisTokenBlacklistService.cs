using FlexFit.Identity.Service.DTOs;
using FlexFit.Identity.Service.Configurations;
using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Caching;
using FlexFit.Identity.Service.Interfaces;

namespace FlexFit.Identity.API.Infrastructure.Redis;

public sealed class RedisTokenBlacklistService : ITokenBlacklistService
{
    private readonly ICacheService _cache;

    public RedisTokenBlacklistService(ICacheService cache)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
    }

    public async Task BlacklistAsync(string jwtId, DateTimeOffset expiresAt, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jwtId))
        {
            throw new ArgumentException("JWT ID cannot be null or whitespace.", nameof(jwtId));
        }

        var ttl = expiresAt - DateTimeOffset.UtcNow;
        if (ttl <= TimeSpan.Zero)
        {
            return;
        }

        var key = RedisKeys.TokenBlacklist(jwtId);
        await _cache.SetAsync(key, true, ttl, cancellationToken);
    }

    public async Task<bool> IsBlacklistedAsync(string jwtId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jwtId))
        {
            return false;
        }

        var key = RedisKeys.TokenBlacklist(jwtId);
        return await _cache.ExistsAsync(key, cancellationToken);
    }
}
