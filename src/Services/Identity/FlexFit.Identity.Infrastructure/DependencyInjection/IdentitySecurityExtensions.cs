using System;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Infrastructure.Email;
using FlexFit.Identity.Infrastructure.Security;

using FlexFit.Identity.Infrastructure.Google;

namespace FlexFit.Identity.Infrastructure.DependencyInjection;

public static class IdentitySecurityExtensions
{
    public static IServiceCollection AddIdentitySecurityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Bind and validate options at startup using data annotations validation
        services.AddOptions<JwtOptions>()
            .Bind(configuration.GetSection(JwtOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<OtpOptions>()
            .Bind(configuration.GetSection(OtpOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<RefreshTokenOptions>()
            .Bind(configuration.GetSection(RefreshTokenOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<LoginSecurityOptions>()
            .Bind(configuration.GetSection(LoginSecurityOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<EmailOptions>()
            .Bind(configuration.GetSection(EmailOptions.SectionName))
            .ValidateDataAnnotations()
            .ValidateOnStart();

        // Register core security services
        services.TryAddScoped<IJwtService, JwtService>();
        services.TryAddScoped<IOtpCacheService, RedisOtpCacheService>();
        services.TryAddScoped<IRefreshTokenCacheService, RedisRefreshTokenCacheService>();
        services.TryAddScoped<ITokenBlacklistService, RedisTokenBlacklistService>();
        services.TryAddScoped<ILoginAttemptService, RedisLoginAttemptService>();
        services.TryAddScoped<IEmailService, MailKitEmailService>();
        services.TryAddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        return services;
    }
}
