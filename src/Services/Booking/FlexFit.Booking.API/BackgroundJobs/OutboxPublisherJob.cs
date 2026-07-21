using FlexFit.Booking.Repository.Data;
using FlexFit.Booking.Repository.Models;
using FlexFit.Booking.Service.Messaging.Events;
using MassTransit;
using Microsoft.EntityFrameworkCore;
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
            var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
            var publishEndpoint = scope.ServiceProvider.GetRequiredService<IPublishEndpoint>();

            // Query up to 20 unprocessed messages
            var messages = await dbContext.OutboxMessages
                .Where(m => m.ProcessedAt == null && m.RetryCount < 5)
                .OrderBy(m => m.OccurredAt)
                .Take(20)
                .ToListAsync(stoppingToken);

            if (!messages.Any()) return;

            _logger.LogInformation("Processing {Count} messages from Outbox database table.", messages.Count);

            foreach (var msg in messages)
            {
                try
                {
                    // Dynamically resolve event type under Events namespace
                    var typeName = $"FlexFit.Booking.Service.Messaging.Events.{msg.EventType}";
                    var eventType = Type.GetType(typeName);

                    if (eventType == null)
                    {
                        throw new InvalidOperationException($"Could not resolve type matching event type: {typeName}");
                    }

                    var eventObj = JsonSerializer.Deserialize(msg.Payload, eventType);
                    if (eventObj == null)
                    {
                        throw new InvalidOperationException($"Failed to deserialize payload for message {msg.OutboxMessageId}");
                    }

                    // Publish via MassTransit publish endpoint
                    await publishEndpoint.Publish(eventObj, eventType, stoppingToken);

                    msg.ProcessedAt = DateTime.UtcNow;
                    msg.ErrorMessage = null;
                }
                catch (Exception ex)
                {
                    msg.RetryCount++;
                    msg.ErrorMessage = ex.Message;
                    _logger.LogError(ex, "Failed to publish Outbox message {MessageId} (Attempt {Count}/5)", msg.OutboxMessageId, msg.RetryCount);
                }
            }

            await dbContext.SaveChangesAsync(stoppingToken);
        }
    }
}
