using System;
using System.Reflection;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using FlexFit.Identity.Application.Common.Behaviors;

namespace FlexFit.Identity.Application.DependencyInjection;

public static class IdentityApplicationExtensions
{
    public static IServiceCollection AddIdentityApplicationServices(
        this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        var currentAssembly = Assembly.GetExecutingAssembly();

        // Register MediatR
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(currentAssembly);
        });

        // Register Pipeline Behaviors in order of execution:
        // 1. Logging Behavior (records entry, success, error)
        // 2. Validation Behavior (validates request models before handling)
        // 3. Transaction Behavior (wraps command executions in DB transaction boundaries)
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(TransactionBehavior<,>));

        // Register FluentValidation validators
        services.AddValidatorsFromAssembly(currentAssembly);

        // Register AutoMapper profiles
        services.AddAutoMapper(currentAssembly);

        return services;
    }
}
