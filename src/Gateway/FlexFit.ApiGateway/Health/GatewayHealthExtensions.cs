using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace FlexFit.ApiGateway.Health;

public static class GatewayHealthExtensions
{
    public static IServiceCollection AddGatewayHealthChecks(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var identityUrl = configuration.GetValue<string>("ReverseProxy:Clusters:identity-cluster:Destinations:identity-api:Address")
                          ?? "http://localhost:5094";

        services.AddHealthChecks()
            .AddCheck("GatewayLive", () => HealthCheckResult.Healthy(), tags: new[] { "live" })
            .AddUrlGroup(
                new Uri($"{identityUrl.TrimEnd('/')}/health/ready"), 
                name: "IdentityServiceReady", 
                tags: new[] { "ready" });

        return services;
    }
}
