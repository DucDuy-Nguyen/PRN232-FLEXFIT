using System;
using System.Text.Json;
using System.Threading.Tasks;
using FlexFit.Payment.Service.Interfaces;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.Payment.API.Infrastructure.Redis
{
    public class RedisCacheService : ICacheService
    {
        private readonly IDatabase _db;
        private readonly ILogger<RedisCacheService> _logger;

        public RedisCacheService(IConnectionMultiplexer redis, ILogger<RedisCacheService> logger)
        {
            _db = redis.GetDatabase();
            _logger = logger;
        }

        public async Task SetAsync<T>(string key, T value, TimeSpan expiration)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                await _db.StringSetAsync(key, json, expiration);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to write key {Key} to Redis cache. Falling back silently.", key);
            }
        }

        public async Task<T?> GetAsync<T>(string key)
        {
            try
            {
                var value = await _db.StringGetAsync(key);
                if (!value.HasValue)
                {
                    return default;
                }
                return JsonSerializer.Deserialize<T>(value!);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to read key {Key} from Redis cache. Falling back to database.", key);
                return default;
            }
        }

        public async Task RemoveAsync(string key)
        {
            try
            {
                await _db.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to remove key {Key} from Redis cache. Falling back silently.", key);
            }
        }
    }
}
