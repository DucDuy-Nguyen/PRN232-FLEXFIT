using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StackExchange.Redis;

namespace FlexFit.RedisEventBus;

public static class RedisEventBusExtensions
{
    public static IServiceCollection AddFlexFitRedisEventBus(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Register IConnectionMultiplexer if not already registered (shared with Caching)
        services.TryAddSingleton<IConnectionMultiplexer>(sp =>
        {
            var connectionString = configuration["Redis:ConnectionString"] 
                ?? configuration.GetConnectionString("RedisConnection")
                ?? "localhost:6379";

            var configurationOptions = ConfigurationOptions.Parse(connectionString);
            configurationOptions.AbortOnConnectFail = false; // Prevents startup crashes on transient network issues
            
            return ConnectionMultiplexer.Connect(configurationOptions);
        });

        // Register Event Bus Services
        services.TryAddScoped<IRedisEventPublisher, RedisEventPublisher>();
        services.TryAddScoped<IRedisEventConsumer, RedisEventConsumer>();
        services.TryAddScoped<IRedisPendingMessageRecovery, RedisPendingMessageRecovery>();
        services.TryAddScoped<IRedisDeadLetterPublisher, RedisDeadLetterPublisher>();

        return services;
    }
}
