using System;
using System.Threading.Tasks;
using FlexFit.Payment.Service.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.Payment.API.Infrastructure.Redis
{
    public class RedisDistributedLockService : IDistributedLockService
    {
        private readonly IDatabase _db;
        private readonly ILogger<RedisDistributedLockService> _logger;

        public RedisDistributedLockService(IConnectionMultiplexer redis, ILogger<RedisDistributedLockService> logger)
        {
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<bool> AcquireLockAsync(string key, string token, TimeSpan expiration)
        {
            try
            {
                return await _db.StringSetAsync(key, token, expiration, When.NotExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acquire lock for key {Key} from Redis.", key);
                return false; // Do not silently fake success
            }
        }

        public async Task<bool> ReleaseLockAsync(string key, string token)
        {
            try
            {
                var luaScript = @"
                    if redis.call('get', KEYS[1]) == ARGV[1] then
                        return redis.call('del', KEYS[1])
                    else
                        return 0
                    end";

                var result = await _db.ScriptEvaluateAsync(luaScript, new RedisKey[] { key }, new RedisValue[] { token });
                return (int)result == 1;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to release lock for key {Key} from Redis.", key);
                return false;
            }
        }
    }
}
