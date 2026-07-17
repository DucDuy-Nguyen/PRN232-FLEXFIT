using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.Caching;

public sealed class RedisDistributedLockService : IDistributedLockService
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisDistributedLockService> _logger;

    public RedisDistributedLockService(
        IConnectionMultiplexer connectionMultiplexer,
        ILogger<RedisDistributedLockService> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IAsyncDisposable?> TryAcquireAsync(
        string resource,
        TimeSpan expiration,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(resource))
        {
            throw new ArgumentException("Resource lock name cannot be null or whitespace.", nameof(resource));
        }

        if (expiration <= TimeSpan.Zero)
        {
            throw new ArgumentException("Lock expiration must be positive.", nameof(expiration));
        }

        var database = _connectionMultiplexer.GetDatabase();
        var lockKey = $"flexfit:lock:{resource}";
        var lockToken = Guid.NewGuid().ToString();

        try
        {
            // SET lockKey lockToken NX PX expiration
            var acquired = await database.StringSetAsync(
                lockKey,
                lockToken,
                expiration,
                when: When.NotExists,
                flags: CommandFlags.None);

            if (acquired)
            {
                _logger.LogDebug("Successfully acquired lock for resource: {Resource} with token: {Token}", resource, lockToken);
                return new RedisDistributedLock(database, lockKey, lockToken, _logger);
            }

            _logger.LogDebug("Failed to acquire lock for resource: {Resource}. Lock is already held.", resource);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acquiring distributed lock for resource: {Resource}", resource);
            throw;
        }
    }

    private sealed class RedisDistributedLock : IAsyncDisposable
    {
        private readonly IDatabase _database;
        private readonly string _lockKey;
        private readonly string _lockToken;
        private readonly ILogger _logger;
        private int _released;

        // Lua script to safely release lock only if the token matches
        private const string ReleaseScript = @"
            if redis.call('get', KEYS[1]) == ARGV[1] then
                return redis.call('del', KEYS[1])
            else
                return 0
            end";

        public RedisDistributedLock(IDatabase database, string lockKey, string lockToken, ILogger logger)
        {
            _database = database;
            _lockKey = lockKey;
            _lockToken = lockToken;
            _logger = logger;
        }

        public async ValueTask DisposeAsync()
        {
            if (Interlocked.CompareExchange(ref _released, 1, 0) == 0)
            {
                try
                {
                    var result = (long)await _database.ScriptEvaluateAsync(
                        ReleaseScript,
                        new RedisKey[] { _lockKey },
                        new RedisValue[] { _lockToken });

                    if (result == 1)
                    {
                        _logger.LogDebug("Successfully released lock: {LockKey} with token: {Token}", _lockKey, _lockToken);
                    }
                    else
                    {
                        _logger.LogWarning("Failed to release lock: {LockKey} with token: {Token}. The lock might have expired and been reclaimed.", _lockKey, _lockToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during distributed lock release: {LockKey}", _lockKey);
                }
            }
        }
    }
}
