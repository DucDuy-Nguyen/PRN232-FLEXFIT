using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.Payment.API.BackgroundServices
{
    public class PendingMessageRecoveryWorker : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<PendingMessageRecoveryWorker> _logger;
        private readonly string _streamName = "flexfit:booking:events";
        private readonly string _groupName = "payment-service";

        public PendingMessageRecoveryWorker(IConnectionMultiplexer redis, ILogger<PendingMessageRecoveryWorker> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PendingMessageRecoveryWorker started.");
            var db = _redis.GetDatabase();

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Check pending messages
                    var pendingInfo = await db.StreamPendingAsync(_streamName, _groupName);
                    if (pendingInfo.PendingMessageCount > 0)
                    {
                        var pendingMessages = await db.StreamPendingMessagesAsync(
                            _streamName,
                            _groupName,
                            count: 10,
                            consumerName: RedisValue.Null
                        );

                        foreach (var msg in pendingMessages)
                        {
                            // Temporarily logged to verify properties
                            _logger.LogInformation("Pending Message Id: {MessageId}, Delivery Count: {Count}", msg.MessageId, msg.DeliveryCount);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PendingMessageRecoveryWorker loop.");
                }

                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken);
            }
        }
    }
}
