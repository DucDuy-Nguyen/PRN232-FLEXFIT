using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using FlexFit.Identity.API.Authorization;

namespace FlexFit.Identity.API.Extensions;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddIdentityAuthorization(this IServiceCollection services)
    {
        services.AddAuthorization(options =>
        {
            options.AddPolicy(IdentityPolicies.AdminOnly, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(IdentityRoles.Admin));

            options.AddPolicy(IdentityPolicies.UserManagement, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(IdentityRoles.Admin, IdentityRoles.Staff));

            options.AddPolicy(IdentityPolicies.AuthenticatedUser, policy =>
                policy.RequireAuthenticatedUser());

            // Resource-based / owner authorization:
            // Currently allows Admin/Staff OR authenticated members (will be fully resolved inside handler/controller context)
            options.AddPolicy(IdentityPolicies.ProfileOwnerOrAdmin, policy =>
                policy.RequireAuthenticatedUser()
                      .RequireRole(IdentityRoles.Admin, IdentityRoles.Staff, IdentityRoles.Member));
        });

        return services;
    }
}
