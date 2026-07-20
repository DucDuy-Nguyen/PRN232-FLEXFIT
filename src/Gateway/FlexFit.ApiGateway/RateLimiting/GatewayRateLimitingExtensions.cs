using System;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FlexFit.ApiGateway.RateLimiting;

public static class GatewayRateLimitingExtensions
{
    public static IServiceCollection AddGatewayRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.OnRejected = async (context, token) =>
            {
                context.HttpContext.Response.ContentType = "application/problem+json";
                
                var problem = new ProblemDetails
                {
                    Status = StatusCodes.Status429TooManyRequests,
                    Title = "Too many requests",
                    Detail = "Rate limit exceeded. Please try again later.",
                    Instance = context.HttpContext.Request.Path,
                    Type = "https://tools.ietf.org/html/rfc6585#section-4"
                };

                await context.HttpContext.Response.WriteAsJsonAsync(problem, cancellationToken: token);
            };

            // 1. Strict Auth Limit (Register, Login, Resend OTP, Reset Password)
            options.AddPolicy("auth-strict", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // 2. Standard Auth Limit (Refresh Token, Verify Email)
            options.AddPolicy("auth-standard", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 15,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // 3. Authenticated requests limit
            options.AddPolicy("authenticated", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.User.Identity?.Name 
                                  ?? httpContext.Connection.RemoteIpAddress?.ToString() 
                                  ?? "unknown-user",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 60,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));

            // 4. Global fallback limit
            options.AddPolicy("global", httpContext =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown-ip",
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 100,
                        Window = TimeSpan.FromMinutes(1),
                        QueueLimit = 0
                    }));
        });

        return services;
    }
}
