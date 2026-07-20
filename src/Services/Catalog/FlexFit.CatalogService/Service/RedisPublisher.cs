using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;
using System;
using System.Text.Json;
using System.Threading.Tasks;

namespace FlexFit.CatalogService.Service;

public class RedisPublisher : IRedisPublisher
{
    private readonly IConnectionMultiplexer? _redis;
    private readonly ILogger<RedisPublisher> _logger;

    public RedisPublisher(IConfiguration configuration, ILogger<RedisPublisher> logger)
    {
        _logger = logger;
        var connectionString = configuration["REDIS_CONNECTION_STRING"] ?? configuration.GetConnectionString("Redis");
        if (!string.IsNullOrEmpty(connectionString))
        {
            try
            {
                _redis = ConnectionMultiplexer.Connect(connectionString);
                _logger.LogInformation("Successfully connected to Redis at {ConnectionString}", connectionString);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to connect to Redis at {ConnectionString}", connectionString);
            }
        }
        else
        {
            _logger.LogWarning("Redis connection string is empty. Redis Streams publishing will be disabled.");
        }
    }

    public async Task PublishAsync<T>(string streamName, string eventType, T eventData)
    {
        var envelope = new
        {
            eventId = Guid.NewGuid(),
            eventType = eventType,
            version = 1,
            occurredAtUtc = DateTime.UtcNow.ToString("o"),
            correlationId = (string?)null,
            data = eventData
        };

        string jsonPayload = JsonSerializer.Serialize(envelope);

        if (_redis == null)
        {
            _logger.LogWarning("Redis is not connected. Event {EventType} with payload {Payload} was NOT published.", eventType, jsonPayload);
            return;
        }

        try
        {
            var db = _redis.GetDatabase();
            await db.StreamAddAsync(streamName, new NameValueEntry[]
            {
                new NameValueEntry("message", jsonPayload)
            });
            _logger.LogInformation("Published event {EventType} to stream {StreamName}", eventType, streamName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error publishing event {EventType} to stream {StreamName}", eventType, streamName);
            throw; // Do not swallow exceptions silently
        }
    }
}
