using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Payment.API.Services.Interfaces;
using FlexFit.Payment.API.Infrastructure.Redis.Interfaces;
using FlexFit.Payment.API.Gateways.Interfaces;
using FlexFit.Payment.API.Repositories.Interfaces;
using FlexFit.Payment.API.Domain.Entities;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace FlexFit.Payment.API.BackgroundServices
{
    public class OutboxPublisherWorker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<OutboxPublisherWorker> _logger;

        public OutboxPublisherWorker(IServiceProvider serviceProvider, ILogger<OutboxPublisherWorker> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("OutboxPublisherWorker started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var outboxRepo = scope.ServiceProvider.GetRequiredService<IOutboxRepository>();
                        var eventPublisher = scope.ServiceProvider.GetRequiredService<IEventPublisher>();

                        var messages = await outboxRepo.GetUnprocessedMessagesAsync();
                        foreach (var message in messages)
                        {
                            var streamName = GetStreamName(message.EventType);
                            _logger.LogInformation("Publishing event {EventType} ({MessageId}) to stream {Stream}", message.EventType, message.Id, streamName);

                            try
                            {
                                // Deserialize payload back to dynamic object or pass as-is.
                                using var doc = JsonDocument.Parse(message.Payload);
                                var payloadObj = doc.RootElement.Clone();

                                await eventPublisher.PublishAsync(streamName, message.EventType, payloadObj, message.Id);
                                await outboxRepo.MarkAsProcessedAsync(message.Id);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Failed to publish outbox message {MessageId}", message.Id);
                                await outboxRepo.LogErrorAsync(message.Id, ex.Message);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred in OutboxPublisherWorker cycle.");
                }

                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        private string GetStreamName(string eventType)
        {
            if (eventType.StartsWith("Payment"))
            {
                return "flexfit:payment:events";
            }
            if (eventType.StartsWith("Credit"))
            {
                return "flexfit:credit:events";
            }
            return "flexfit:general:events";
        }
    }
}


