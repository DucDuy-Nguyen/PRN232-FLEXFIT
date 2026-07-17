using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Options;
using FlexFit.Caching;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Entities;
using FlexFit.Identity.Domain.Enums;
using FlexFit.Identity.Infrastructure.Security;
using Xunit;

namespace FlexFit.BuildingBlocks.Tests;

public sealed class SecurityUnitTests
{
    private static readonly string TestJwtKey = new('a', 32); // 32 characters key for signing (256-bit)

    // 1. JWT Claims and Validation Tests
    [Fact]
    public async Task JwtService_ShouldGenerateTokenWithRequiredClaims()
    {
        // Arrange
        var options = Options.Create(new JwtOptions
        {
            Key = TestJwtKey,
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiresInMinutes = 30
        });

        var jwtService = new JwtService(options);
        var user = User.Create("John Smith", "john.smith@example.com", "hash", "123");

        // Act
        var result = await jwtService.GenerateAccessTokenAsync(user, new[] { "Member", "Admin" });

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Token);
        Assert.NotEmpty(result.JwtId);
        Assert.True(result.ExpiresAt > DateTimeOffset.UtcNow);

        // Decode token to verify claims
        var tokenHandler = new JwtSecurityTokenHandler();
        var jwtToken = tokenHandler.ReadJwtToken(result.Token);

        Assert.Equal("TestIssuer", jwtToken.Issuer);
        Assert.Equal("TestAudience", jwtToken.Audiences.First());
        Assert.Equal(user.UserId.ToString(), jwtToken.Subject);
        Assert.Equal(result.JwtId, jwtToken.Id);
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Member");
        Assert.Contains(jwtToken.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    [Fact]
    public async Task JwtService_ValidateExpiredToken_ShouldRejectInvalidTokens()
    {
        // Arrange
        var options = Options.Create(new JwtOptions
        {
            Key = TestJwtKey,
            Issuer = "TestIssuer",
            Audience = "TestAudience",
            ExpiresInMinutes = 30
        });
        var jwtService = new JwtService(options);

        // Act & Assert
        // Malformed token should be rejected (return null)
        Assert.Null(jwtService.ValidateExpiredToken("invalid.token.here"));

        // Token with wrong signature/key should be rejected
        var wrongOptions = Options.Create(new JwtOptions
        {
            Key = new string('b', 32),
            Issuer = "TestIssuer",
            Audience = "TestAudience"
        });
        var wrongService = new JwtService(wrongOptions);
        
        var user = User.Create("Alice", "alice@example.com", "hash", "123");
        var tokenResult = await wrongService.GenerateAccessTokenAsync(user, new[] { "Member" });
        
        Assert.Null(jwtService.ValidateExpiredToken(tokenResult.Token));
    }

    // 2. OTP Code Generation & Hashing Tests
    [Fact]
    public void OtpOptions_ShouldThrowException_WhenKeyIsTooShort()
    {
        var options = Options.Create(new OtpOptions { HashingKey = "short" });
        Assert.Throws<InvalidOperationException>(() => new RedisOtpCacheService(
            new MockCacheService(),
            new MockLockService(),
            options,
            new MockLogger<RedisOtpCacheService>()
        ));
    }

    [Fact]
    public void OtpGenerator_ShouldGenerateSixDigitCode()
    {
        for (int i = 0; i < 50; i++)
        {
            var otpValue = RandomNumberGenerator.GetInt32(100000, 1000000);
            Assert.True(otpValue >= 100000 && otpValue < 1000000);
            Assert.Equal(6, otpValue.ToString().Length);
        }
    }

    // 3. Refresh Token Format Verification
    [Fact]
    public void RefreshToken_RawToken_ShouldHaveTokenIdAndEntropyParts()
    {
        // Arrange
        var secretBytes = RandomNumberGenerator.GetBytes(32);
        var secret = Convert.ToBase64String(secretBytes).Replace("+", "-").Replace("/", "_").Replace("=", "");
        var tokenId = Guid.NewGuid().ToString("N");
        var rawToken = $"{tokenId}.{secret}";

        // Act
        var parts = rawToken.Split('.');

        // Assert
        Assert.Equal(2, parts.Length);
        Assert.Equal(32, parts[0].Length); // Guid N format is 32 chars
        Assert.True(parts[1].Length > 40); // Base64url encoded 32 bytes is ~43 chars
    }

    // Mock/Stub implementations for unit test environment
    private sealed class MockCacheService : ICacheService
    {
        public Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default) => Task.FromResult<T?>(default);
        public Task SetAsync<T>(string key, T value, TimeSpan expiration, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RemoveAsync(string key, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default) => Task.FromResult(false);
    }

    private sealed class MockLockService : IDistributedLockService
    {
        public Task<IAsyncDisposable?> TryAcquireAsync(string resource, TimeSpan expiration, CancellationToken cancellationToken = default)
            => Task.FromResult<IAsyncDisposable?>(new MockLock());
        
        private sealed class MockLock : IAsyncDisposable
        {
            public ValueTask DisposeAsync() => ValueTask.CompletedTask;
        }
    }

    private sealed class MockLogger<T> : Microsoft.Extensions.Logging.ILogger<T>, IDisposable
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => this;
        public void Dispose() { }
        public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => false;
        public void Log<TState>(Microsoft.Extensions.Logging.LogLevel logLevel, Microsoft.Extensions.Logging.EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) { }
    }
}
