using System;
using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using FlexFit.Identity.Service.Interfaces;
using FlexFit.Identity.Service.Services;

namespace FlexFit.Identity.API.Extensions;

public static class AuthenticationExtensions
{
    public static IServiceCollection AddIdentityJwtAuthentication(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var jwtSection = configuration.GetSection("Jwt");
        var keyStr = jwtSection.GetValue<string>("Key") 
                     ?? throw new InvalidOperationException("JWT Secret Key is not configured.");
        
        var issuer = jwtSection.GetValue<string>("Issuer") 
                     ?? throw new InvalidOperationException("JWT Issuer is not configured.");
        
        var audience = jwtSection.GetValue<string>("Audience") 
                       ?? throw new InvalidOperationException("JWT Audience is not configured.");

        var keyBytes = Encoding.UTF8.GetBytes(keyStr);

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
                ClockSkew = TimeSpan.Zero // Set to zero to enforce strict expiration immediately
            };

            options.Events = new JwtBearerEvents
            {
                OnTokenValidated = async context =>
                {
                    var jtiClaim = context.Principal?.FindFirst(JwtRegisteredClaimNames.Jti);
                    if (jtiClaim == null)
                    {
                        context.Fail("JWT has no JTI identifier.");
                        return;
                    }

                    var blacklistService = context.HttpContext.RequestServices
                        .GetRequiredService<ITokenBlacklistService>();

                    var isBlacklisted = await blacklistService.IsBlacklistedAsync(
                        jtiClaim.Value, 
                        context.HttpContext.RequestAborted);

                    if (isBlacklisted)
                    {
                        context.Fail("Token has been revoked/blacklisted.");
                    }
                }
            };
        });

        // Register CurrentUserService
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, API.Authentication.CurrentUserService>();

        return services;
    }
}
