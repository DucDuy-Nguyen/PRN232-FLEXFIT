using System;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace FlexFit.ApiGateway.Authentication;

public static class GatewayAuthenticationExtensions
{
    public static IServiceCollection AddGatewayAuthentication(
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
                ClockSkew = TimeSpan.Zero
            };
        });

        return services;
    }
}
