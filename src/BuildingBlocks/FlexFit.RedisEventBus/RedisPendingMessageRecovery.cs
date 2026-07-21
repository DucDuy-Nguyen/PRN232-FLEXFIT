using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.RedisEventBus;

public sealed class RedisPendingMessageRecovery : IRedisPendingMessageRecovery
{
    private readonly IConnectionMultiplexer _connectionMultiplexer;
    private readonly ILogger<RedisPendingMessageRecovery> _logger;

    public RedisPendingMessageRecovery(IConnectionMultiplexer connectionMultiplexer, ILogger<RedisPendingMessageRecovery> logger)
    {
        _connectionMultiplexer = connectionMultiplexer ?? throw new ArgumentNullException(nameof(connectionMultiplexer));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<IReadOnlyCollection<RedisEventMessage>> ClaimStaleMessagesAsync(
        string stream,
        string consumerGroup,
        string consumerName,
        TimeSpan minimumIdleTime,
        int count,
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
            throw new ArgumentException("Recovery count must be positive.", nameof(count));
        }

        var database = _connectionMultiplexer.GetDatabase();

        try
        {
            // 1. XPENDING stream consumerGroup - + count [any-consumer]
            //    Pass RedisValue.Null to get pending messages across all consumers (not just one consumer)
            var pendingInfos = await database.StreamPendingMessagesAsync(
                stream,
                consumerGroup,
                count,
                consumerName: RedisValue.Null, // Null = all consumers
                minId: "-",
                maxId: "+");

            if (pendingInfos == null || pendingInfos.Length == 0)
            {
                return Array.Empty<RedisEventMessage>();
            }

            // 2. Filter messages whose idle time exceeds minimumIdleTime
            // StreamPendingMessageInfo.IdleTimeInMilliseconds is the correct property in SE.Redis 2.6
            var staleMessageIds = pendingInfos
                .Where(info => info.IdleTimeInMilliseconds >= minimumIdleTime.TotalMilliseconds)
                .Select(info => info.MessageId)
                .ToArray();

            if (staleMessageIds.Length == 0)
            {
                return Array.Empty<RedisEventMessage>();
            }

            // 3. Claim ownership using XCLAIM: stream consumerGroup consumerName minIdleTimeInMs messageId1 messageId2...
            var claimedEntries = await database.StreamClaimAsync(
                stream,
                consumerGroup,
                consumerName,
                (long)minimumIdleTime.TotalMilliseconds,
                staleMessageIds);

            if (claimedEntries == null || claimedEntries.Length == 0)
            {
                return Array.Empty<RedisEventMessage>();
            }

            _logger.LogInformation("Successfully claimed {ClaimedCount} stale messages from stream {Stream} for group {ConsumerGroup}", 
                claimedEntries.Length, stream, consumerGroup);

            var messages = new List<RedisEventMessage>(claimedEntries.Length);
            foreach (var entry in claimedEntries)
            {
                var message = RedisEventConsumer.MapToRedisEventMessage(entry);
                if (message != null)
                {
                    messages.Add(message);
                }
            }

            return messages;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to recover pending messages for stream {Stream} and group {ConsumerGroup}", stream, consumerGroup);
            throw;
        }
    }
}
