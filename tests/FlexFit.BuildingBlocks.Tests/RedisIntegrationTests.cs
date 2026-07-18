using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using FlexFit.Caching;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Enums;
using FlexFit.Identity.Domain.Exceptions;
using FlexFit.Identity.Infrastructure.Security;
using Xunit;

namespace FlexFit.BuildingBlocks.Tests;

public sealed class RedisIntegrationTests : IDisposable
{
    private readonly bool _redisAvailable;
    private readonly ConnectionMultiplexer? _connection;
    private readonly RedisCacheService? _cacheService;
    private readonly RedisDistributedLockService? _lockService;
    private readonly RedisOtpCacheService? _otpCacheService;
    private readonly RedisRefreshTokenCacheService? _refreshTokenCacheService;
    private readonly RedisLoginAttemptService? _loginAttemptService;
    private readonly RedisEventPublisher? _publisher;
    private readonly RedisEventConsumer? _consumer;
    private readonly RedisPendingMessageRecovery? _recovery;
    private readonly RedisDeadLetterPublisher? _dlqPublisher;
    
    private const string RedisConnectionString = "localhost:6379";

    public RedisIntegrationTests()
    {
        try
        {
            var options = ConfigurationOptions.Parse(RedisConnectionString);
            options.ConnectTimeout = 1000; // 1 second timeout
            options.AbortOnConnectFail = true;

            _connection = ConnectionMultiplexer.Connect(options);
            _redisAvailable = _connection.IsConnected;

            if (_redisAvailable)
            {
                var loggerFactory = LoggerFactory.Create(builder => builder.AddConsole());
                
                // Caching building blocks setup
                var mockDistOptions = Options.Create(new RedisOptions
                {
                    ConnectionString = RedisConnectionString,
                    InstanceName = "flexfit-test:"
                });

                // Clear/cleanup before running test suite to isolate test data
                var db = _connection.GetDatabase();
                var endpoints = _connection.GetEndPoints();
                var server = _connection.GetServer(endpoints.First());
                // Avoid FLUSHALL, clean keys with test prefixes instead
                var keys = server.Keys(pattern: "flexfit-test:*").ToArray();
                if (keys.Length > 0)
                {
                    db.KeyDelete(keys);
                }

                var distCache = new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCache(new Microsoft.Extensions.Caching.StackExchangeRedis.RedisCacheOptions
                {
                    Configuration = RedisConnectionString,
                    InstanceName = "flexfit-test:"
                });

                _cacheService = new RedisCacheService(distCache, loggerFactory.CreateLogger<RedisCacheService>());
                _lockService = new RedisDistributedLockService(_connection, loggerFactory.CreateLogger<RedisDistributedLockService>());

                // Identity Security services setup
                var otpOpts = Options.Create(new OtpOptions
                {
                    HashingKey = "otp-hashing-key-test-123456",
                    ExpiryInMinutes = 2,
                    CooldownInSeconds = 5,
                    MaxFailedAttempts = 3
                });

                _otpCacheService = new RedisOtpCacheService(
                    _cacheService,
                    _lockService,
                    otpOpts,
                    loggerFactory.CreateLogger<RedisOtpCacheService>());

                var refreshOpts = Options.Create(new RefreshTokenOptions
                {
                    ExpiryInDays = 1
                });
                _refreshTokenCacheService = new RedisRefreshTokenCacheService(
                    _connection,
                    _cacheService,
                    _lockService,
                    refreshOpts,
                    loggerFactory.CreateLogger<RedisRefreshTokenCacheService>());

                var loginOpts = Options.Create(new LoginSecurityOptions
                {
                    MaxFailedAttempts = 3,
                    LockoutDurationInMinutes = 2
                });
                _loginAttemptService = new RedisLoginAttemptService(
                    _connection,
                    loginOpts,
                    loggerFactory.CreateLogger<RedisLoginAttemptService>());

                // EventBus blocks setup
                _publisher = new RedisEventPublisher(_connection, loggerFactory.CreateLogger<RedisEventPublisher>());
                _consumer = new RedisEventConsumer(_connection, loggerFactory.CreateLogger<RedisEventConsumer>());
                _recovery = new RedisPendingMessageRecovery(_connection, loggerFactory.CreateLogger<RedisPendingMessageRecovery>());
                _dlqPublisher = new RedisDeadLetterPublisher(_connection, loggerFactory.CreateLogger<RedisDeadLetterPublisher>());
            }
        }
        catch
        {
            _redisAvailable = false;
            Console.WriteLine(">>> WARNING: Redis is not running on localhost:6379. Integration tests are skipped. <<<");
        }
    }

    public void Dispose()
    {
        _connection?.Dispose();
    }

    // A helper method for conditional tests
    private bool SkipTest()
    {
        if (!_redisAvailable)
        {
            Console.WriteLine("Redis Integration test skipped (Redis server offline)");
            return true;
        }
        return false;
    }

    // 1. Basic Cache Integration
    [Fact]
    public async Task Cache_SetAndGet_ShouldSucceed()
    {
        if (SkipTest()) return;

        // Arrange
        var key = "flexfit-test:caching:test-key";
        var value = "Hello Redis Cache";

        // Act
        await _cacheService!.SetAsync(key, value, TimeSpan.FromSeconds(30));
        var retrieved = await _cacheService.GetAsync<string>(key);
        var exists = await _cacheService.ExistsAsync(key);

        await _cacheService.RemoveAsync(key);
        var existsAfterDelete = await _cacheService.ExistsAsync(key);

        // Assert
        Assert.True(exists);
        Assert.Equal(value, retrieved);
        Assert.False(existsAfterDelete);
    }

    // 2. Lock Ownership and Release Lua script test
    [Fact]
    public async Task Lock_ShouldBeExclusiveAndReleasedByLuaScript()
    {
        if (SkipTest()) return;

        // Arrange
        var resource = "user-profile-99";

        // Act
        var firstLock = await _lockService!.TryAcquireAsync(resource, TimeSpan.FromSeconds(5));
        var secondLock = await _lockService.TryAcquireAsync(resource, TimeSpan.FromSeconds(5));

        // Assert firstLock succeeds, secondLock gets null
        Assert.NotNull(firstLock);
        Assert.Null(secondLock);

        // Act release lock
        await firstLock.DisposeAsync();

        // Second lock should now succeed
        var thirdLock = await _lockService.TryAcquireAsync(resource, TimeSpan.FromSeconds(5));
        Assert.NotNull(thirdLock);

        await thirdLock.DisposeAsync();
    }

    // 3. OTP Flow Integration
    [Fact]
    public async Task OtpFlow_ShouldSupportValidationAttemptsAndLifecycle()
    {
        if (SkipTest()) return;

        var email = "verify-user@flexfit.com";
        var purpose = OtpPurpose.VerifyEmail;

        // Act generate OTP
        var plaintextOtp = await _otpCacheService!.CreateOtpAsync(email, purpose);
        
        // Cooldown check
        var isInCooldown = await _otpCacheService.IsInCooldownAsync(email, purpose);
        Assert.True(isInCooldown);

        // Act validate wrong OTP code
        var validationResult1 = await _otpCacheService.ValidateOtpAsync(email, purpose, "000000");
        Assert.Equal(OtpValidationResult.Invalid, validationResult1);

        // Act validate correct OTP code
        var validationResult2 = await _otpCacheService.ValidateOtpAsync(email, purpose, plaintextOtp);
        Assert.Equal(OtpValidationResult.Valid, validationResult2);

        // Try validation after success (should be NotFound / deleted)
        var validationResult3 = await _otpCacheService.ValidateOtpAsync(email, purpose, plaintextOtp);
        Assert.Equal(OtpValidationResult.NotFound, validationResult3);
    }

    // 4. Refresh Token Lifecycle and Reuse detection
    [Fact]
    public async Task RefreshTokenFlow_ShouldSupportRotationAndReuseAttackDetection()
    {
        if (SkipTest()) return;

        var userId = Guid.NewGuid();

        // 1. Create token
        var createResult = await _refreshTokenCacheService!.CreateAsync(userId);
        Assert.NotNull(createResult);

        // 2. Validate token
        var validated = await _refreshTokenCacheService.ValidateAsync(createResult.RawToken);
        Assert.Equal(createResult.TokenId, validated.TokenId);
        Assert.Equal(createResult.FamilyId, validated.FamilyId);
        Assert.False(validated.IsRevoked);

        // 3. Rotate token
        var rotateResult = await _refreshTokenCacheService.RotateAsync(createResult.RawToken);
        Assert.NotNull(rotateResult);
        Assert.NotEqual(createResult.RawToken, rotateResult.RawToken);
        Assert.Equal(createResult.FamilyId, rotateResult.FamilyId);

        // 4. Replay old token (should trigger REUSE ATTACK DETECTION and revoke family)
        await Assert.ThrowsAsync<RefreshTokenReuseException>(async () => 
            await _refreshTokenCacheService.ValidateAsync(createResult.RawToken));

        // 5. New token should be revoked now too
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(async () =>
            await _refreshTokenCacheService.ValidateAsync(rotateResult.RawToken));
    }

    // 5. Login Attempts Atomic Rate Limit Test
    [Fact]
    public async Task LoginAttempts_ShouldIncrementAndLockoutUser()
    {
        if (SkipTest()) return;

        var email = "failed-user@flexfit.com";

        // Act: 3 failure limit configured
        var r1 = await _loginAttemptService!.RecordFailureAsync(email);
        Assert.Equal(1, r1.FailedAttempts);
        Assert.False(r1.IsBlocked);

        var r2 = await _loginAttemptService.RecordFailureAsync(email);
        Assert.Equal(2, r2.FailedAttempts);
        Assert.False(r2.IsBlocked);

        var r3 = await _loginAttemptService.RecordFailureAsync(email);
        Assert.Equal(3, r3.FailedAttempts);
        Assert.True(r3.IsBlocked);

        var isBlocked = await _loginAttemptService.IsBlockedAsync(email);
        Assert.True(isBlocked);

        // Reset
        await _loginAttemptService.ResetAsync(email);
        var isBlockedAfterReset = await _loginAttemptService.IsBlockedAsync(email);
        Assert.False(isBlockedAfterReset);
    }

    // 6. Redis Streams EventBus Publishing, Reading and Recovery Tests
    [Fact]
    public async Task RedisStreams_PublisherAndConsumerGroup_ShouldOperateSuccessfully()
    {
        if (SkipTest()) return;

        var streamName = "flexfit-test:events:stream-integration";
        var groupName = "test-group";
        var consumerName = "test-consumer-1";

        // Act setup group
        await _consumer!.EnsureConsumerGroupAsync(streamName, groupName);

        // Act publish event
        var testEvent = new EmailVerifiedEvent
        {
            UserId = Guid.NewGuid(),
            Email = "verified@example.com"
        };
        var messageId = await _publisher!.PublishAsync(streamName, testEvent);
        Assert.NotEmpty(messageId);

        // Act read event
        var messages = await _consumer.ReadAsync(streamName, groupName, consumerName, count: 5, blockTime: TimeSpan.FromMilliseconds(50));
        Assert.Single(messages);
        
        var message = messages.First();
        Assert.Equal(messageId, message.Id);
        Assert.Equal(testEvent.EventId, message.EventId);
        Assert.Equal("EmailVerifiedEvent", message.EventType);

        // Act Acknowledge event
        await _consumer.AcknowledgeAsync(streamName, groupName, messageId);

        // Test recovery details (should have 0 pending now since it was ACK-ed)
        var recovered = await _recovery!.ClaimStaleMessagesAsync(streamName, groupName, consumerName, TimeSpan.FromMilliseconds(5), count: 5);
        Assert.Empty(recovered);
    }

    // 7. Dead Letter Publisher Mapping Test
    [Fact]
    public async Task DeadLetterPublisher_ShouldPublishFailedMessage()
    {
        if (SkipTest()) return;

        // Arrange
        var dlm = new RedisDeadLetterMessage(
            OriginalStream: "flexfit-test:events:incoming",
            OriginalMessageId: "1726532562-0",
            EventId: Guid.NewGuid(),
            EventType: "EmailVerifiedEvent",
            Payload: "{}",
            RetryCount: 5,
            ErrorSummary: "Deserialization error",
            FailedAt: DateTimeOffset.UtcNow,
            ConsumerGroup: "test-group",
            ConsumerName: "consumer-1",
            CorrelationId: "corr-id"
        );

        // Act
        var dlqMessageId = await _dlqPublisher!.PublishAsync(dlm);

        // Assert
        Assert.NotEmpty(dlqMessageId);
    }
}
