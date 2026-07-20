using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using FlexFit.Caching;
using FlexFit.Identity.API.Services.Interfaces;

namespace FlexFit.Identity.API.Services.Implementations;

public sealed class RedisLoginAttemptService : ILoginAttemptService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly LoginSecurityOptions _options;
    private readonly ILogger<RedisLoginAttemptService> _logger;

    public RedisLoginAttemptService(
        IConnectionMultiplexer connectionMultiplexer,
        IOptions<LoginSecurityOptions> options,
        ILogger<RedisLoginAttemptService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LoginAttemptResult> RecordFailureAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(email);
        var key = RedisKeys.LoginAttempts(normalizedEmail);
        var db = _connectionMultiplexer.GetDatabase();

        try
        {
            var count = await db.StringIncrementAsync(key);

            if (count == 1)
            {
                await db.KeyExpireAsync(key, TimeSpan.FromMinutes(_options.LockoutDurationInMinutes));
            }

            var isBlocked = count >= _options.MaxFailedAttempts;
            TimeSpan? remaining = null;

            if (isBlocked)
            {
                remaining = await db.KeyTimeToLiveAsync(key);
                _logger.LogWarning("Account {Email} has been locked due to {Attempts} failed attempts. Lockout time remaining: {Remaining}", 
                    normalizedEmail, count, remaining);
            }

            return new LoginAttemptResult((int)count, isBlocked, remaining);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording login failure for {Email}", normalizedEmail);
            throw;
        }
    }

    public async Task ResetAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(email);
        var key = RedisKeys.LoginAttempts(normalizedEmail);
        var db = _connectionMultiplexer.GetDatabase();

        try
        {
            await db.KeyDeleteAsync(key);
            _logger.LogInformation("Successfully reset login attempts for {Email}", normalizedEmail);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error resetting login attempts for {Email}", normalizedEmail);
            throw;
        }
    }

    public async Task<bool> IsBlockedAsync(string email, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(email);
        var key = RedisKeys.LoginAttempts(normalizedEmail);
        var db = _connectionMultiplexer.GetDatabase();

        try
        {
            var value = await db.StringGetAsync(key);
            if (value.IsNullOrEmpty)
            {
                return false;
            }

            if (int.TryParse(value, out var count))
            {
                return count >= _options.MaxFailedAttempts;
            }

            return false;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking lockout status for {Email}", normalizedEmail);
            throw;
        }
    }
}
