using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using FlexFit.Payment.API.Services.Interfaces;
using FlexFit.Payment.API.Infrastructure.Redis.Interfaces;
using FlexFit.Payment.API.Gateways.Interfaces;
using StackExchange.Redis;

namespace FlexFit.Payment.API.Infrastructure.Redis
{
    public class RedisService : IRedisService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IDatabase _db;

        public RedisService(IConnectionMultiplexer redis)
        {
            _redis = redis;
            _db = _redis.GetDatabase();
        }

        public async Task<bool> AcquireLockAsync(string key, string value, TimeSpan expiration)
        {
            return await _db.StringSetAsync(key, value, expiration, When.NotExists);
        }

        public async Task<bool> ReleaseLockAsync(string key, string value)
        {
            var luaScript = @"
                if redis.call('get', KEYS[1]) == ARGV[1] then
                    return redis.call('del', KEYS[1])
                else
                    return 0
                end";

            var result = await _db.ScriptEvaluateAsync(luaScript, new RedisKey[] { key }, new RedisValue[] { value });
            return (int)result == 1;
        }

        public async Task<bool> IsIdempotentAsync(string key, TimeSpan expiry)
        {
            return await _db.StringSetAsync(key, "processed", expiry, When.NotExists);
        }

        public async Task SetWalletBalanceAsync(Guid userId, int balance)
        {
            var key = $"payment:user:{userId}:balance";
            await _db.StringSetAsync(key, balance, TimeSpan.FromMinutes(30));
        }

        public async Task<int?> GetWalletBalanceAsync(Guid userId)
        {
            var key = $"payment:user:{userId}:balance";
            var val = await _db.StringGetAsync(key);
            if (val.HasValue && int.TryParse(val, out var balance))
            {
                return balance;
            }
            return null;
        }

        public async Task InvalidateWalletBalanceAsync(Guid userId)
        {
            var key = $"payment:user:{userId}:balance";
            await _db.KeyDeleteAsync(key);
        }

        public async Task InvalidateRevenueCacheAsync()
        {
            await _db.KeyDeleteAsync("payment:admin:revenue_summary");
        }

        public async Task PublishToStreamAsync(string streamName, string eventType, string payload, Guid? correlationId = null)
        {
            var values = new List<NameValueEntry>
            {
                new NameValueEntry("EventId", Guid.NewGuid().ToString()),
                new NameValueEntry("EventType", eventType),
                new NameValueEntry("OccurredAt", DateTime.UtcNow.ToString("O")),
                new NameValueEntry("CorrelationId", (correlationId ?? Guid.NewGuid()).ToString()),
                new NameValueEntry("Producer", "FlexFit.PaymentService"),
                new NameValueEntry("SchemaVersion", "1.0"),
                new NameValueEntry("Payload", payload)
            };

            await _db.StreamAddAsync(streamName, values.ToArray());
        }
    }
}


