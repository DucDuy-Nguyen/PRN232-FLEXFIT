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
    public class RedisEventPublisher : IEventPublisher
    {
        private readonly IDatabase _db;

        public RedisEventPublisher(IConnectionMultiplexer redis)
        {
            _db = redis.GetDatabase();
        }

        public async Task PublishAsync<T>(string streamName, string eventType, T eventPayload, Guid? correlationId = null)
        {
            var options = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
            var jsonPayload = JsonSerializer.Serialize(eventPayload, options);

            var values = new List<NameValueEntry>
            {
                new NameValueEntry("EventId", Guid.NewGuid().ToString()),
                new NameValueEntry("EventType", eventType),
                new NameValueEntry("OccurredAt", DateTime.UtcNow.ToString("O")),
                new NameValueEntry("CorrelationId", (correlationId ?? Guid.NewGuid()).ToString()),
                new NameValueEntry("Producer", "FlexFit.PaymentService"),
                new NameValueEntry("SchemaVersion", "1.0"),
                new NameValueEntry("Payload", jsonPayload)
            };

            await _db.StreamAddAsync(streamName, values.ToArray());
        }
    }
}


