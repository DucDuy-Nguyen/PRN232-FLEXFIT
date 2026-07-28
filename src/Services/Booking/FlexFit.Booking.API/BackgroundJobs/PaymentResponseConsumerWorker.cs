using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Booking.Service.Messaging.Events;
using FlexFit.Booking.Service.Service.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace FlexFit.Booking.API.BackgroundJobs
{
    public class PaymentResponseConsumerWorker : BackgroundService
    {
        private readonly IConnectionMultiplexer _redis;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<PaymentResponseConsumerWorker> _logger;
        private readonly string _streamName = "flexfit:credit:events";
        private readonly string _groupName = "booking-service";
        private readonly string _consumerName;

        public PaymentResponseConsumerWorker(
            IConnectionMultiplexer redis,
            IServiceProvider serviceProvider,
            ILogger<PaymentResponseConsumerWorker> logger)
        {
            _redis = redis;
            _serviceProvider = serviceProvider;
            _logger = logger;
            _consumerName = $"booking-consumer-{Guid.NewGuid()}";
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("PaymentResponseConsumerWorker starting listening on {Stream}.", _streamName);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var db = _redis.GetDatabase();

                    try
                    {
                        await db.StreamCreateConsumerGroupAsync(_streamName, _groupName, "0-0", createStream: true);
                    }
                    catch (RedisServerException ex) when (ex.Message.Contains("already exists") || ex.Message.Contains("BUSYGROUP"))
                    {
                        // Group exists
                    }

                    var messages = await db.StreamReadGroupAsync(
                        _streamName,
                        _groupName,
                        _consumerName,
                        ">",
                        count: 5
                    );

                    if (messages != null && messages.Length > 0)
                    {
                        foreach (var message in messages)
                        {
                            await ProcessMessagePayloadAsync(db, message);
                        }
                    }
                    else
                    {
                        await Task.Delay(1000, stoppingToken);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in PaymentResponseConsumerWorker run cycle. Retrying in 5 seconds...");
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

        private async Task ProcessMessagePayloadAsync(IDatabase db, StreamEntry message)
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

            if (!string.IsNullOrEmpty(eventType) && !string.IsNullOrEmpty(payloadJson))
            {
                using var scope = _serviceProvider.CreateScope();
                var paymentHandler = scope.ServiceProvider.GetRequiredService<IBookingPaymentHandler>();
                var jsonOptions = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                var eventId = Guid.NewGuid();

                if (eventType == "CreditDeductionSucceeded")
                {
                    var succ = JsonSerializer.Deserialize<PaymentResultPayload>(payloadJson, jsonOptions);
                    if (succ != null)
                    {
                        var evt = new CrpZEAWYtiB6bJ16NuLbGCc6CZ6jJdKfb63
                        {
                            BookingId = succ.BookingId,
                            BookingType = succ.ReferenceType?.Contains("Class") == true ? "CLASS" : "GYM",
                            UserId = succ.UserId,
                            CorrelationId = succ.BookingId
                        };
                        await paymentHandler.HandlePaymentCompletedAsync(evt, eventId);
                        _logger.LogInformation("Successfully handled CreditDeductionSucceeded for Booking {BookingId}", succ.BookingId);
                    }
                }
                else if (eventType == "CreditDeductionFailed")
                {
                    var fail = JsonSerializer.Deserialize<PaymentFailPayload>(payloadJson, jsonOptions);
                    if (fail != null)
                    {
                        var evt = new CreditDeductionFailedEvent
                        {
                            BookingId = fail.BookingId,
                            BookingType = fail.ReferenceType?.Contains("Class") == true ? "CLASS" : "GYM",
                            UserId = fail.UserId,
                            Reason = fail.Reason ?? "Payment failed",
                            CorrelationId = fail.BookingId
                        };
                        await paymentHandler.HandlePaymentFailedAsync(evt, eventId);
                        _logger.LogWarning("Successfully handled CreditDeductionFailed for Booking {BookingId}", fail.BookingId);
                    }
                }
            }

            try
            {
                await db.StreamAcknowledgeAsync(_streamName, _groupName, message.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to acknowledge message {MessageId}", message.Id);
            }
        }

        private class PaymentResultPayload
        {
            public Guid BookingId { get; set; }
            public Guid UserId { get; set; }
            public int CreditCost { get; set; }
            public string? ReferenceType { get; set; }
        }

        private class PaymentFailPayload
        {
            public Guid BookingId { get; set; }
            public Guid UserId { get; set; }
            public string? Reason { get; set; }
            public string? ReferenceType { get; set; }
        }
    }
}
