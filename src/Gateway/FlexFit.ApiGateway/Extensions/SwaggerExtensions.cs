using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;

namespace FlexFit.ApiGateway.Extensions;

public static class SwaggerExtensions
{
    public static IServiceCollection AddGatewaySwagger(this IServiceCollection services)
    {
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "FlexFit API Gateway",
                Version = "v1",
                Description = "Unified gateway routing interface for FlexFit Microservices."
            });
        });

        return services;
    }
}
