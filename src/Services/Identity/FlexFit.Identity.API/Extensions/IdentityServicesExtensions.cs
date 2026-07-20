using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FlexFit.Identity.API.Data;
using FlexFit.Identity.API.Data.Repositories.Implementations;
using FlexFit.Identity.API.Services.Interfaces;
using FlexFit.Identity.API.Services.Implementations;
using FlexFit.Identity.API.Services.Interfaces;
using FlexFit.Identity.API.Data;
using FlexFit.Identity.API.Services.Implementations;
using FlexFit.Identity.API.Services.Implementations;
using FlexFit.Identity.API.Services.Implementations;
using FlexFit.Identity.API.Data.Repositories.Interfaces;

namespace FlexFit.Identity.API.Extensions;

public static class IdentityServicesExtensions
{
    /// <summary>
    /// Registers all Identity service dependencies: EF DbContext, repositories,
    /// infrastructure services (JWT, OTP, Email, Redis), and business services.
    /// Replaces the multi-project AddIdentityPersistence / AddIdentitySecurityServices / AddIdentityApplicationServices.
    /// </summary>
    public static IServiceCollection AddIdentityServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // ─── EF Core ─────────────────────────────────────────────────────────────
        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? configuration["ConnectionStrings:IdentityDatabase"];

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("IdentityDatabase connection string is required.");

        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOpts =>
            {
                sqlOpts.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                sqlOpts.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            }));

        // ─── Repositories & Unit of Work ─────────────────────────────────────────
        services.TryAddScoped<IUserRepository, UserRepository>();
        services.TryAddScoped<IRoleRepository, RoleRepository>();
        services.TryAddScoped<IMemberProfileRepository, MemberProfileRepository>();
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        // ─── Password Hasher ──────────────────────────────────────────────────────
        services.TryAddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        // ─── Options ──────────────────────────────────────────────────────────────
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

        // ─── Infrastructure Services (now in API.Services.Implementations) ────────
        services.TryAddScoped<IJwtService, JwtService>();
        services.TryAddScoped<IOtpCacheService, RedisOtpCacheService>();
        services.TryAddScoped<IRefreshTokenCacheService, RedisRefreshTokenCacheService>();
        services.TryAddScoped<ITokenBlacklistService, RedisTokenBlacklistService>();
        services.TryAddScoped<ILoginAttemptService, RedisLoginAttemptService>();
        services.TryAddScoped<IEmailService, MailKitEmailService>();
        services.TryAddScoped<IGoogleTokenValidator, GoogleTokenValidator>();

        // ─── Business / Application Services ─────────────────────────────────────
        services.TryAddScoped<IAuthService, AuthService>();
        services.TryAddScoped<IUserService, UserService>();
        services.TryAddScoped<IProfileService, ProfileService>();

        return services;
    }
}
