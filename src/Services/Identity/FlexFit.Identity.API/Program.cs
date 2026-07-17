using FlexFit.Caching;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.DependencyInjection;
using FlexFit.Identity.Infrastructure.DependencyInjection;
using FlexFit.Identity.API.Extensions;
using FlexFit.Identity.API.Middleware;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add building blocks & persistence services to the container.
builder.Services.AddFlexFitRedisCaching(builder.Configuration);
builder.Services.AddFlexFitRedisEventBus(builder.Configuration);
builder.Services.AddIdentityPersistence(builder.Configuration);
builder.Services.AddIdentitySecurityServices(builder.Configuration);
builder.Services.AddIdentityApplicationServices();

// API security, versioning, healthchecks, and swagger registration
builder.Services.AddIdentityJwtAuthentication(builder.Configuration);
builder.Services.AddIdentityAuthorization();
builder.Services.AddIdentitySwagger();
builder.Services.AddIdentityHealthChecks();

builder.Services.AddControllers();

var app = builder.Build();

// Exception Handling boundary at the start of pipeline
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<CorrelationIdMiddleware>();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlexFit Identity API v1");
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Register Health check endpoints
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = check => check.Tags.Contains("ready")
});

app.Run();

// Exposed entry point for integration tests using WebApplicationFactory
public partial class Program { }
