using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Contracts;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.RedisEventBus;

public sealed class RedisEventPublisher : IRedisEventPublisher
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisEventPublisher> _logger;

    public RedisEventPublisher(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisEventPublisher> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> PublishAsync<TEvent>(
        string stream,
        TEvent integrationEvent,
        CancellationToken cancellationToken = default)
        where TEvent : class, IIntegrationEvent
    {
        if (string.IsNullOrWhiteSpace(stream))
        {
            throw new ArgumentException("Stream name cannot be null or whitespace.", nameof(stream));
        }

        if (integrationEvent == null)
        {
            throw new ArgumentNullException(nameof(integrationEvent));
        }

        var database = _connectionMultiplexer.GetDatabase();

        try
        {
            var payloadJson = JsonSerializer.Serialize(integrationEvent);

            var values = new NameValueEntry[]
            {
                new("eventId", integrationEvent.EventId.ToString()),
                new("eventType", integrationEvent.EventType),
                new("payload", payloadJson),
                new("version", integrationEvent.Version),
                new("retryCount", 0), // Starts with 0
                new("occurredAt", integrationEvent.OccurredAt.ToString("o")), // ISO 8601 format
                new("correlationId", integrationEvent.CorrelationId ?? string.Empty),
                new("causationId", integrationEvent.CausationId ?? string.Empty)
            };

            // StreamAddAsync uses XADD command
            var messageId = await database.StreamAddAsync(stream, values);
            
            _logger.LogInformation("Published event {EventType} to stream {Stream} with message ID {MessageId}", 
                integrationEvent.EventType, stream, messageId);

            return messageId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish event to Redis stream {Stream}", stream);
            throw;
        }
    }
}
