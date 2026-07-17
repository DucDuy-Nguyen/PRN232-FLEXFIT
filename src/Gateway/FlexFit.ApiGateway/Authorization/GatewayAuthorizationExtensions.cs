using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;

namespace FlexFit.ApiGateway.Authorization;

public static class GatewayAuthorizationExtensions
{
    public static IServiceCollection AddGatewayAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(GatewayPolicies.GatewayAuthenticated, policy =>
                policy.RequireAuthenticatedUser());

            options.AddPolicy(GatewayPolicies.GatewayAdminOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Admin"));

            options.AddPolicy(GatewayPolicies.GatewayUserManagement, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole("Admin", "Staff"));
        });

        return services;
    }
}
