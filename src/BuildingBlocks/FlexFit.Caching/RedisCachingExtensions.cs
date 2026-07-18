using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

namespace FlexFit.Caching;

public static class RedisCachingExtensions
{
    public static IServiceCollection AddFlexFitRedisCaching(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        var section = configuration.GetSection(RedisOptions.SectionName);
        var connectionString = section[nameof(RedisOptions.ConnectionString)]
            ?? string.Empty;
        var instanceName = section[nameof(RedisOptions.InstanceName)]
            ?? "flexfit:";

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Redis:ConnectionString is required. Ensure it is set in configuration.");
        }

        // Bind options for downstream consumers — uses IConfiguration overload from ConfigurationExtensions
        services.Configure<RedisOptions>(configuration.GetSection(RedisOptions.SectionName));

        // Register shared IConnectionMultiplexer singleton only once.
        // Both Caching and EventBus use TryAddSingleton so whichever registers first wins,
        // preventing duplicate connections.
        services.TryAddSingleton<IConnectionMultiplexer>(_ =>
        {
            var opts = ConfigurationOptions.Parse(connectionString);
            opts.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(opts);
        });

        // Register IDistributedCache backed by Redis
        services.AddStackExchangeRedisCache(opt =>
        {
            opt.Configuration = connectionString;
            opt.InstanceName = instanceName;
        });

        // Register ICacheService and IDistributedLockService
        services.TryAddScoped<ICacheService, RedisCacheService>();
        services.TryAddScoped<IDistributedLockService, RedisDistributedLockService>();

        return services;
    }
}
