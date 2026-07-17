using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.RedisEventBus;

public sealed class RedisDeadLetterPublisher : IRedisDeadLetterPublisher
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisDeadLetterPublisher> _logger;

    public RedisDeadLetterPublisher(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisDeadLetterPublisher> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<string> PublishAsync(
        RedisDeadLetterMessage message,
        CancellationToken cancellationToken = default)
    {
        if (message == null)
        {
            throw new ArgumentNullException(nameof(message));
        }

        var database = _connectionMultiplexer.GetDatabase();

        try
        {
            var values = new NameValueEntry[]
            {
                new("originalStream", message.OriginalStream),
                new("originalMessageId", message.OriginalMessageId),
                new("eventId", message.EventId.ToString()),
                new("eventType", message.EventType),
                new("payload", message.Payload),
                new("retryCount", message.RetryCount),
                new("errorSummary", message.ErrorSummary),
                new("failedAt", message.FailedAt.ToString("o")),
                new("consumerGroup", message.ConsumerGroup),
                new("consumerName", message.ConsumerName),
                new("correlationId", message.CorrelationId ?? string.Empty)
            };

            // StreamAddAsync uses XADD command on the dead letter stream
            var messageId = await database.StreamAddAsync(RedisStreams.DeadLetterEvents, values);
            
            _logger.LogWarning("Event {EventId} from stream {OriginalStream} failed after {RetryCount} retries and was moved to dead-letter stream with message ID {MessageId}", 
                message.EventId, message.OriginalStream, message.RetryCount, messageId);

            return messageId.ToString();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish message to dead-letter stream for Event ID {EventId}", message.EventId);
            throw;
        }
    }
}
