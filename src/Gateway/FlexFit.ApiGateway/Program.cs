using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using FlexFit.ApiGateway.Authentication;
using FlexFit.ApiGateway.Authorization;
using FlexFit.ApiGateway.Extensions;
using FlexFit.ApiGateway.Health;
using FlexFit.ApiGateway.Middleware;
using FlexFit.ApiGateway.Options;
using FlexFit.ApiGateway.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Register configuration settings options
builder.Services.Configure<GatewayOptions>(builder.Configuration.GetSection(GatewayOptions.SectionName));

// Configure services & extensions
builder.Services.AddProblemDetails();
builder.Services.AddGatewayAuthentication(builder.Configuration);
builder.Services.AddGatewayAuthorization();
builder.Services.AddGatewayRateLimiting();
builder.Services.AddGatewayHealthChecks(builder.Configuration);
builder.Services.AddGatewaySwagger();

// Configure CORS policy based on configurations
var gatewayOptions = builder.Configuration.GetSection(GatewayOptions.SectionName).Get<GatewayOptions>();
var allowedOrigins = gatewayOptions?.AllowedOrigins?.ToArray() ?? new[] { "http://localhost:5173" };
builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy.WithOrigins(allowedOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Configure Kestrel limits
var bodyLimit = gatewayOptions?.RequestBodyLimitBytes ?? 1048576;
builder.WebHost.ConfigureKestrel(options =>
{
    options.Limits.MaxRequestBodySize = bodyLimit;
});

// Configure YARP Reverse Proxy
builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

var app = builder.Build();

// Gateway global exception filter
app.UseMiddleware<GatewayExceptionMiddleware>();

// Standard forwarded headers for proxy setups
app.UseForwardedHeaders();

// Middlewares tracking execution flows
app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<RequestLoggingMiddleware>();

app.UseCors();
app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

// Swagger endpoints configuration
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlexFit API Gateway v1");
        c.SwaggerEndpoint("/swagger/identity/swagger.json", "Identity Service API");
    });
}

// Health checks routing
app.MapHealthChecks("/health/live", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("live")
});
app.MapHealthChecks("/health/ready", new HealthCheckOptions
{
    Predicate = r => r.Tags.Contains("ready")
});

app.MapReverseProxy();

app.Run();

// Declare partial Program to allow WebApplicationFactory test server bindings
public partial class Program { }
