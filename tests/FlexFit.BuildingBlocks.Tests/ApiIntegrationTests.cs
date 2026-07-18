using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using NSubstitute;
using FlexFit.Caching;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Infrastructure.Persistence;
using FlexFit.Identity.API.Contracts.Authentication;
using FlexFit.Identity.API.Authorization;
using Xunit;

namespace FlexFit.BuildingBlocks.Tests;

public sealed class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    private readonly IUserRepository _userRepository = Substitute.For<IUserRepository>();
    private readonly IRoleRepository _roleRepository = Substitute.For<IRoleRepository>();
    private readonly IMemberProfileRepository _profileRepository = Substitute.For<IMemberProfileRepository>();
    private readonly IPasswordHasher _passwordHasher = Substitute.For<IPasswordHasher>();
    private readonly IOtpCacheService _otpCache = Substitute.For<IOtpCacheService>();
    private readonly IRefreshTokenCacheService _refreshTokenCache = Substitute.For<IRefreshTokenCacheService>();
    private readonly ILoginAttemptService _loginAttempt = Substitute.For<ILoginAttemptService>();
    private readonly IEmailService _emailService = Substitute.For<IEmailService>();
    private readonly IRedisEventPublisher _eventPublisher = Substitute.For<IRedisEventPublisher>();
    private readonly ITokenBlacklistService _blacklistService = Substitute.For<ITokenBlacklistService>();
    private readonly StackExchange.Redis.IConnectionMultiplexer _redis = Substitute.For<StackExchange.Redis.IConnectionMultiplexer>();

    public ApiIntegrationTests(WebApplicationFactory<Program> factory)
    {
        Environment.SetEnvironmentVariable("Redis__ConnectionString", "localhost:6379");
        Environment.SetEnvironmentVariable("Jwt__Key", new string('x', 32));
        Environment.SetEnvironmentVariable("Jwt__Issuer", "TestIssuer");
        Environment.SetEnvironmentVariable("Jwt__Audience", "TestAudience");
        
        Environment.SetEnvironmentVariable("Otp__HashingKey", "SomeSecretKeyForHashingOtpValue123");
        Environment.SetEnvironmentVariable("EmailSettings__Host", "smtp.mailtrap.io");
        Environment.SetEnvironmentVariable("EmailSettings__Port", "2525");
        Environment.SetEnvironmentVariable("EmailSettings__Username", "user");
        Environment.SetEnvironmentVariable("EmailSettings__Password", "pass");
        Environment.SetEnvironmentVariable("EmailSettings__SenderEmail", "noreply@flexfit.com");
        Environment.SetEnvironmentVariable("EmailSettings__SenderName", "FlexFit");

        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureTestServices(services =>
            {
                // Register mock abstractions in place of actual SQL / Redis runtimes
                services.AddScoped(_ => _userRepository);
                services.AddScoped(_ => _roleRepository);
                services.AddScoped(_ => _profileRepository);
                services.AddScoped(_ => _passwordHasher);
                services.AddScoped(_ => _otpCache);
                services.AddScoped(_ => _refreshTokenCache);
                services.AddScoped(_ => _loginAttempt);
                services.AddScoped(_ => _emailService);
                services.AddScoped(_ => _eventPublisher);
                services.AddScoped(_ => _blacklistService);
                services.AddSingleton(_ => _redis);

                // Configure SQLite In-Memory Database for testing IdentityDbContext mapping
                var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<IdentityDbContext>));
                if (descriptor != null) services.Remove(descriptor);

                services.AddDbContext<IdentityDbContext>(options =>
                {
                    options.UseSqlite("DataSource=:memory:");
                });
            });
        });
    }

    // 1. Health Checks tests
    [Fact]
    public async Task Get_HealthLive_ShouldReturnHealthy()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/health/live");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var content = await response.Content.ReadAsStringAsync();
        Assert.Contains("Healthy", content);
    }

    // 2. Swagger availability in dev / default environments
    [Fact]
    public async Task Get_SwaggerIndex_ShouldReturnSuccess()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync("/swagger/v1/swagger.json");

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    // 3. Register Validation - Returns 400 ProblemDetails on invalid fields
    [Fact]
    public async Task Post_Register_ShouldReturn400ProblemDetails_WhenPasswordIsTooShort()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new RegisterRequest("invalid-email", "123", "123", "Short");

        // Act
        var response = await client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);

        var problem = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Validation failed", problem.GetProperty("title").GetString());
        Assert.True(problem.TryGetProperty("errors", out var errors));
        Assert.True(errors.TryGetProperty("Email", out _));
        Assert.True(errors.TryGetProperty("Password", out _));
    }

    // 4. Protected Endpoints: Return 401 Unauthorized without Token
    [Fact]
    public async Task Get_UserById_ShouldReturn401_WhenAnonymous()
    {
        // Arrange
        var client = _factory.CreateClient();

        // Act
        var response = await client.GetAsync($"/api/v1/users/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    // 5. Correlation ID middleware validation
    [Fact]
    public async Task Request_ShouldReturnCorrelationIdHeader()
    {
        // Arrange
        var client = _factory.CreateClient();
        var request = new HttpRequestMessage(HttpMethod.Get, "/health/live");
        request.Headers.Add("X-Correlation-ID", "custom-id-1234");

        // Act
        var response = await client.SendAsync(request);

        // Assert
        Assert.True(response.Headers.Contains("X-Correlation-ID"));
        var headerVal = string.Join("", response.Headers.GetValues("X-Correlation-ID"));
        Assert.Equal("custom-id-1234", headerVal);
    }
}
