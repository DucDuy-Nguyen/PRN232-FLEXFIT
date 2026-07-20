using System;
using System.Threading.Tasks;
using FlexFit.Payment.API.Interfaces.Services;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.Payment.API.Infrastructure.Redis
{
    public class RedisIdempotencyService : IIdempotencyService
    {
        private readonly IDatabase _db;
        private readonly ILogger<RedisIdempotencyService> _logger;

        public RedisIdempotencyService(IConnectionMultiplexer redis, ILogger<RedisIdempotencyService> logger)
        {
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task<bool> IsIdempotentAsync(string key, TimeSpan expiry)
        {
            try
            {
                // If the key is successfully set, it means the request hasn't been processed yet.
                // So it returns true. Otherwise, returns false.
                return await _db.StringSetAsync(key, "processed", expiry, When.NotExists);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to check/set idempotency key {Key} in Redis. Falling back to SQL Server processed messages.", key);
                return true; // Return true to proceed to SQL which will handle duplicate check via ProcessedMessages
            }
        }
    }
}
