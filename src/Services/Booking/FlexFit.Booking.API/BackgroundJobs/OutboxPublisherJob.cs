using FlexFit.Booking.Repository.Models;
using FlexFit.Booking.Repository.Repositories.Interfaces;
using FlexFit.Booking.Service.Messaging.Events;
using MassTransit;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Booking.API.BackgroundJobs
{
    public class OutboxPublisherJob : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<OutboxPublisherJob> _logger;

        public OutboxPublisherJob(
            IServiceScopeFactory scopeFactory,
            ILogger<OutboxPublisherJob> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Flexfit Outbox Publisher Job initialized.");

            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(5)); // Scan outbox table every 5 seconds

            while (await timer.WaitForNextTickAsync(stoppingToken) && !stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await ProcessOutboxMessagesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred while processing Outbox messages.");
                }
            }
        }

        private async Task ProcessOutboxMessagesAsync(CancellationToken stoppingToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();
            var redisConnection = scope.ServiceProvider.GetService<StackExchange.Redis.IConnectionMultiplexer>();

            // Query up to 20 unprocessed messages
            var messages = await outboxRepo.GetUnprocessedMessagesAsync(20, stoppingToken);

            if (!messages.Any()) return;

            _logger.LogInformation("Processing {Count} messages from Outbox database table.", messages.Count);

            foreach (var msg in messages)
            {
                try
                {
                    if (redisConnection != null && (msg.EventType == "CreditDeductionRequested" || msg.EventType == "CreditRefundRequested"))
                    {
                        // Publish to Redis Stream for Payment Service
                        var db = redisConnection.GetDatabase();
                        await db.StreamAddAsync("flexfit:booking:events", new StackExchange.Redis.NameValueEntry[]
                        {
                            new StackExchange.Redis.NameValueEntry("EventType", msg.EventType),
                            new StackExchange.Redis.NameValueEntry("Payload", msg.Payload)
                        });

                        _logger.LogInformation("Successfully published {EventType} for booking {AggregateId} to Redis Stream flexfit:booking:events", msg.EventType, msg.AggregateId);
                    }
                    else
                    {
                        // Dynamically resolve event type under Events namespace for MassTransit
                        var typeName = $"FlexFit.Booking.Service.Messaging.Events.{msg.EventType}";
                        var eventType = Type.GetType(typeName);

                        if (eventType != null)
                        {
                            var eventObj = JsonSerializer.Deserialize(msg.Payload, eventType);
                            if (eventObj != null)
                            {
                                await publishEndpoint.Publish(eventObj, eventType, stoppingToken);
                            }
                        }
                    }

                    msg.ProcessedAt = DateTime.UtcNow;
                    msg.ErrorMessage = null;
                }
                catch (Exception ex)
                {
                    msg.RetryCount++;
                    msg.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "Failed to publish Outbox message {MessageId} (Attempt {Count}/5)", msg.OutboxMessageId, msg.RetryCount);
                }

                await outboxRepo.UpdateOutboxMessageAsync(msg);
            }

            await outboxRepo.SaveChangesAsync(stoppingToken);
        }
    }
}
