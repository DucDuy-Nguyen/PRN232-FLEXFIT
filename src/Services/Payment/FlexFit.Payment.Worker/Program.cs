using FlexFit.Payment.Application.Interfaces;
using FlexFit.Payment.Application.Services;
using FlexFit.Payment.Infrastructure.Data;
using FlexFit.Payment.Infrastructure.Repositories;
using FlexFit.Payment.Infrastructure.Services;
using FlexFit.Payment.Worker.Workers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);

// Register DbContext (SQL Server)
builder.Services.AddDbContext<PaymentDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Register Redis Connection Multiplexer
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379,abortConnect=false";
    var options = ConfigurationOptions.Parse(connString);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});

// Register Repositories & Services
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ICreditRepository, CreditRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();

builder.Services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IIdempotencyService, RedisIdempotencyService>();
builder.Services.AddScoped<IEventPublisher, RedisEventPublisher>();

builder.Services.AddScoped<ICreditAdjustmentService, CreditAdjustmentService>();

// Register Hosted Services
builder.Services.AddHostedService<OutboxPublisherWorker>();
builder.Services.AddHostedService<RedisConsumerWorker>();
builder.Services.AddHostedService<PendingMessageRecoveryWorker>();

var host = builder.Build();
host.Run();
