using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using FlexFit.Identity.API.Data;

namespace FlexFit.Identity.API.Extensions;

public static class HealthCheckExtensions
{
    public static IServiceCollection AddIdentityHealthChecks(this IServiceCollection services)
    {
        services.AddHealthChecks()
            .AddCheck("Self", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
            .AddCheck<ReadyHealthCheck>("ReadyCheck", tags: new[] { "ready" });

        return services;
    }
}

public sealed class ReadyHealthCheck : IHealthCheck
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConnectionMultiplexer _redis;

    public ReadyHealthCheck(IServiceProvider serviceProvider, IConnectionMultiplexer redis)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        _redis = redis ?? throw new ArgumentNullException(nameof(redis));
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            // 1. Check Redis Connection
            if (!_redis.IsConnected)
            {
                return HealthCheckResult.Unhealthy("Redis server is not reachable.");
            }

            // 2. Check Database Connection
            using var scope = _serviceProvider.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
            var canConnect = await dbContext.Database.CanConnectAsync(cancellationToken);
            if (!canConnect)
            {
                return HealthCheckResult.Unhealthy("Database server is not reachable.");
            }

            return HealthCheckResult.Healthy("All backend services are ready.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Ready health check failed.", ex);
        }
    }
}
