using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.RedisEventBus;

public sealed class RedisEventConsumer : IRedisEventConsumer
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisEventConsumer> _logger;

    public RedisEventConsumer(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisEventConsumer> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task EnsureConsumerGroupAsync(
        string stream,
        string consumerGroup,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stream))
        {
            throw new ArgumentException("Stream name cannot be null or whitespace.", nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(consumerGroup))
        {
            throw new ArgumentException("Consumer group cannot be null or whitespace.", nameof(consumerGroup));
        }

        var database = _connectionMultiplexer.GetDatabase();

        try
        {
            // XGROUP CREATE stream consumerGroup $ MKSTREAM
            // StreamPosition.NewMessages = '$'. The 4th positional bool = createStream (MKSTREAM).
            // Named parameter 'makeStream' does not exist in SE.Redis 2.6 — use positional.
            await database.StreamCreateConsumerGroupAsync(
                stream,
                consumerGroup,
                StreamPosition.NewMessages,
                true);    // createStream = MKSTREAM

            _logger.LogInformation("Successfully created consumer group {ConsumerGroup} on stream {Stream}", consumerGroup, stream);
        }
        catch (RedisServerException ex) when (ex.Message.Contains("BUSYGROUP", StringComparison.OrdinalIgnoreCase))
        {
            // Group already exists, which is not an error for startup
            _logger.LogDebug("Consumer group {ConsumerGroup} already exists on stream {Stream}", consumerGroup, stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating consumer group {ConsumerGroup} on stream {Stream}", consumerGroup, stream);
            throw;
        }
    }

    public async Task<IReadOnlyCollection<RedisEventMessage>> ReadAsync(
        string stream,
        string consumerGroup,
        string consumerName,
        int count,
        TimeSpan blockTime,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stream))
        {
            throw new ArgumentException("Stream name cannot be null or whitespace.", nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(consumerGroup))
        {
            throw new ArgumentException("Consumer group cannot be null or whitespace.", nameof(consumerGroup));
        }

        if (string.IsNullOrWhiteSpace(consumerName))
        {
            throw new ArgumentException("Consumer name cannot be null or whitespace.", nameof(consumerName));
        }

        if (count <= 0)
        {
            throw new ArgumentException("Read count must be positive.", nameof(count));
        }

        var database = _connectionMultiplexer.GetDatabase();

        try
        {
            // XREADGROUP GROUP consumerGroup consumerName COUNT count STREAMS stream >
            // Note: StackExchange.Redis 2.6 does NOT support blocking XREADGROUP (BLOCK option).
            // The multiplexed connection model prevents blocking reads.
            // Use the string literal ">" as position for undelivered new messages.
            // The blockTime parameter is accepted in the interface for future compatibility
            // (e.g., swapping to a non-multiplexed client) but not applied here.
            var entries = await database.StreamReadGroupAsync(
                stream,
                consumerGroup,
                consumerName,
                ">",       // Equivalent to StreamPosition.Undelivered
                count,
                noAck: false);

            if (entries == null || entries.Length == 0)
            {
                return Array.Empty<RedisEventMessage>();
            }

            var messages = new List<RedisEventMessage>(entries.Length);
            foreach (var entry in entries)
            {
                var message = MapToRedisEventMessage(entry);
                if (message != null)
                {
                    messages.Add(message);
                }
            }

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error reading from stream {Stream} for group {ConsumerGroup}", stream, consumerGroup);
            throw;
        }
    }

    public async Task AcknowledgeAsync(
        string stream,
        string consumerGroup,
        string messageId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(stream))
        {
            throw new ArgumentException("Stream name cannot be null or whitespace.", nameof(stream));
        }

        if (string.IsNullOrWhiteSpace(consumerGroup))
        {
            throw new ArgumentException("Consumer group cannot be null or whitespace.", nameof(consumerGroup));
        }

        if (string.IsNullOrWhiteSpace(messageId))
        {
            throw new ArgumentException("Message ID cannot be null or whitespace.", nameof(messageId));
        }

        var database = _connectionMultiplexer.GetDatabase();

        try
        {
            // XACK stream consumerGroup messageId
            var acknowledged = await database.StreamAcknowledgeAsync(stream, consumerGroup, messageId);
            if (acknowledged == 1)
            {
                _logger.LogDebug("Successfully acknowledged message {MessageId} in group {ConsumerGroup} for stream {Stream}", 
                    messageId, consumerGroup, stream);
            }
            else
            {
                _logger.LogWarning("Message {MessageId} was not acknowledged (possibly already acknowledged or deleted) in stream {Stream}", 
                    messageId, stream);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging message {MessageId} in stream {Stream}", messageId, stream);
            throw;
        }
    }

    internal static RedisEventMessage? MapToRedisEventMessage(StreamEntry entry)
    {
        var dict = entry.Values.ToDictionary(x => x.Name.ToString(), x => x.Value.ToString());

        if (!dict.TryGetValue("eventId", out var eventIdStr) || !Guid.TryParse(eventIdStr, out var eventId))
        {
            return null;
        }

        dict.TryGetValue("eventType", out var eventType);
        dict.TryGetValue("payload", out var payload);
        dict.TryGetValue("version", out var versionStr);
        dict.TryGetValue("retryCount", out var retryCountStr);
        dict.TryGetValue("occurredAt", out var occurredAtStr);
        dict.TryGetValue("correlationId", out var correlationId);
        dict.TryGetValue("causationId", out var causationId);

        _ = int.TryParse(versionStr, out var version);
        _ = int.TryParse(retryCountStr, out var retryCount);
        _ = DateTimeOffset.TryParse(occurredAtStr, out var occurredAt);

        return new RedisEventMessage(
            entry.Id.ToString(),
            eventId,
            eventType ?? string.Empty,
            payload ?? string.Empty,
            version == 0 ? 1 : version,
            retryCount,
            occurredAt == default ? DateTimeOffset.UtcNow : occurredAt,
            correlationId,
            causationId);
    }
}
