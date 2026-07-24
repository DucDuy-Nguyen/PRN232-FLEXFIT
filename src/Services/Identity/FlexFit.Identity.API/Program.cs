using FlexFit.Caching;
using FlexFit.RedisEventBus;
using FlexFit.Identity.API.Extensions;
using FlexFit.Identity.API.Middleware;
using FlexFit.Identity.Repository.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

var builder = WebApplication.CreateBuilder(args);

// Add building blocks & persistence services to the container.
builder.Services.AddFlexFitRedisCaching(builder.Configuration);
builder.Services.AddFlexFitRedisEventBus(builder.Configuration);

// Consolidated flat-MVC DI: registers EF, repositories, security, email, and business services
builder.Services.AddIdentityServices(builder.Configuration);

// API security, versioning, healthchecks, and swagger registration
builder.Services.AddIdentityJwtAuthentication(builder.Configuration);
builder.Services.AddIdentityAuthorization();
builder.Services.AddIdentitySwagger();
builder.Services.AddIdentityHealthChecks();

builder.Services.AddControllers();

var app = builder.Build();

// Ensure database and tables are created on startup
using (var scope = app.Services.CreateScope())
{
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<IdentityDbContext>();
        var dbCreator = dbContext.Database.GetService<IRelationalDatabaseCreator>();
        if (!dbCreator.Exists())
        {
            dbCreator.Create();
        }
        
        try
        {
            _ = dbContext.Users.FirstOrDefault();
        }
        catch
        {
            dbCreator.CreateTables();
            logger.LogInformation("Successfully created Identity tables and seed data.");
        }
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Failed to ensure Identity database creation.");
    }
}

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
