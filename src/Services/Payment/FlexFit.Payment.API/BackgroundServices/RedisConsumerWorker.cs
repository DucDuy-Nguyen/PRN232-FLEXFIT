using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Payment.API.Services.Interfaces;
using FlexFit.Payment.API.Infrastructure.Redis.Interfaces;
using FlexFit.Payment.API.Gateways.Interfaces;
using FlexFit.Payment.API.DTOs.Events;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.Payment.API.BackgroundServices
{
    public class RedisConsumerWorker : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<RedisConsumerWorker> _logger;
        private readonly string _streamName = "flexfit:booking:events";
        private readonly string _groupName = "payment-service";
        private readonly string _consumerName;

        public RedisConsumerWorker(
            IConnectionMultiplexer redis,
            IServiceProvider serviceProvider,
            ILogger<RedisConsumerWorker> logger)
        {
            _redis = redis;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _consumerName = $"payment-consumer-{Guid.NewGuid()}";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("RedisConsumerWorker starting.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var db = _redis.GetDatabase();

                    // Ensure consumer group exists
                    try
                    {
                        await db.StreamCreateConsumerGroupAsync(_streamName, _groupName, "0-0", createStream: true);
                    }
                    catch (RedisServerException ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("BUSYGROUP"))
                    {
                        // Group already exists
                    }

                    var messages = await db.StreamReadGroupAsync(
                        _streamName,
                        _groupName,
                        _consumerName,
                        ">",
                        count: 1
                    );

                    if (messages != null && messages.Length > 0)
                    {
                        foreach (var message in messages)
                        {
                            await HandleMessageWithRetryAsync(db, message, stoppingToken);
                        }
                    }
                    else
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in RedisConsumerWorker run cycle. Retrying in 5 seconds...");
                    try
                    {
                        await Task.Delay(5000, stoppingToken);
                    }
                    catch (TaskCanceledException)
                    {
                        break;
                    }
                }
            }
        }

        private async Task HandleMessageWithRetryAsync(IDatabase db, StreamEntry message, CancellationToken stoppingToken)
        {
            int attempt = 0;
            bool success = false;
            Exception? lastException = null;

            while (attempt < 3 && !success && !stoppingToken.IsCancellationRequested)
            {
                attempt++;
                try
                {
                    await ProcessMessagePayloadAsync(message);
                    success = true;
                }
                catch (Exception ex)
                {
                    lastException = ex;
                    _logger.LogError(ex, "Error processing stream message {MessageId} on attempt {Attempt}", message.Id, attempt);
                    if (attempt < 3)
                    {
                        await Task.Delay(10, stoppingToken);
                    }
                }
            }

            if (!success)
            {
                _logger.LogError(lastException, "Failed to process message {MessageId} after {Attempts} attempts. Moving to dead-letter stream.", message.Id, attempt);
                try
                {
                    // Publish to dead-letter
                    await db.StreamAddAsync("flexfit:dead-letter", message.Values);
                }
                catch (Exception dlEx)
                {
                    _logger.LogError(dlEx, "Failed to write message {MessageId} to dead-letter stream.", message.Id);
                }
            }

            // Always acknowledge so we don't block
            try
            {
                await db.StreamAcknowledgeAsync(_streamName, _groupName, message.Id);
            }
            catch (Exception ackEx)
            {
                _logger.LogError(ackEx, "Failed to acknowledge message {MessageId}", message.Id);
            }
        }

        private async Task ProcessMessagePayloadAsync(StreamEntry message)
        {
            string? eventType = null;
            string? payloadJson = null;

            foreach (var entry in message.Values)
            {
                if (entry.Name == "EventType")
                {
                    eventType = entry.Value;
                }
                else if (entry.Name == "Payload")
                {
                    payloadJson = entry.Value;
                }
            }

            if (string.IsNullOrEmpty(eventType) || string.IsNullOrEmpty(payloadJson))
            {
                _logger.LogWarning("Discarding message {MessageId} due to missing EventType or Payload", message.Id);
                return;
            }

            using (var scope = _serviceProvider.CreateScope())
            {
                var adjustmentService = scope.ServiceProvider.GetRequiredService<ICreditAdjustmentService>();
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };

                if (eventType == "CreditDeductionRequested")
                {
                    var req = JsonSerializer.Deserialize<CreditDeductionRequested>(payloadJson, jsonOptions);
                    if (req != null)
                    {
                        await adjustmentService.DeductCreditAsync(
                            req.BookingId,
                            req.UserId,
                            req.CreditCost,
                            req.ReferenceType ?? "GymBooking",
                            req.Description ?? "Deduction for gym session"
                        );
                    }
                }
                else if (eventType == "CreditRefundRequested")
                {
                    var req = JsonSerializer.Deserialize<CreditRefundRequested>(payloadJson, jsonOptions);
                    if (req != null)
                    {
                        await adjustmentService.RefundCreditAsync(
                            req.BookingId,
                            req.UserId,
                            req.RefundCredit,
                            req.ReferenceType ?? "GymBooking",
                            req.Description ?? "Refund booking"
                        );
                    }
                }
                else
                {
                    _logger.LogWarning("Unknown event type: {EventType} in message {MessageId}", eventType, message.Id);
                }
            }
        }
    }
}


