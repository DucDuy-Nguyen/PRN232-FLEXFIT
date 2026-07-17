using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Infrastructure.Persistence;
using FlexFit.Identity.Infrastructure.Persistence.Repositories;
using FlexFit.Identity.Infrastructure.Security;

namespace FlexFit.Identity.Infrastructure.DependencyInjection;

public static class IdentityPersistenceExtensions
{
    public static IServiceCollection AddIdentityPersistence(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));
        if (configuration == null) throw new ArgumentNullException(nameof(configuration));

        // Get connection string from Configuration (supporting ConnectionStrings:IdentityDatabase or ConnectionStrings__IdentityDatabase)
        var connectionString = configuration.GetConnectionString("IdentityDatabase")
            ?? configuration["ConnectionStrings:IdentityDatabase"];

        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException("IdentityDatabase connection string is required and was not found in configuration.");
        }

        // Register EF DbContext
        services.AddDbContext<IdentityDbContext>(options =>
            options.UseSqlServer(connectionString, sqlOpts =>
            {
                sqlOpts.MigrationsAssembly(typeof(IdentityDbContext).Assembly.FullName);
                sqlOpts.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(30), errorNumbersToAdd: null);
            }));

        // Repositories and Unit of Work
        services.TryAddScoped<IUserRepository, UserRepository>();
        services.TryAddScoped<IRoleRepository, RoleRepository>();
        services.TryAddScoped<IMemberProfileRepository, MemberProfileRepository>();
        services.TryAddScoped<IUnitOfWork, UnitOfWork>();

        // Password Hasher
        services.TryAddSingleton<IPasswordHasher, Pbkdf2PasswordHasher>();

        return services;
    }
}
