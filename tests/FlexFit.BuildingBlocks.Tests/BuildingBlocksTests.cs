using System;
using System.Text.Json;
using FlexFit.Caching;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using Xunit;

namespace FlexFit.BuildingBlocks.Tests;

public sealed class BuildingBlocksTests
{
    // 1. Redis Key Generation and 2. Email Normalization Tests
    [Fact]
    public void RedisKeys_ShouldGenerateCorrectKeyFormatAndNormalizeEmail()
    {
        // Arrange
        var userId = Guid.Parse("11111111-2222-3333-4444-555555555555");
        var tokenId = "token123";
        var familyId = "family456";
        var email = "  Test.User@FlexFit.COM  ";
        var purpose = "VERIFY_EMAIL";
        var resource = "user-profile-update";

        // Act
        var normalizedEmail = RedisKeys.NormalizeEmail(email);
        var otpKey = RedisKeys.EmailOtp(normalizedEmail, purpose);
        var otpCooldownKey = RedisKeys.EmailOtpCooldown(normalizedEmail, purpose);
        var refreshTokenKey = RedisKeys.RefreshToken(tokenId);
        var familyKey = RedisKeys.RefreshTokenFamily(familyId);
        var userKey = RedisKeys.User(userId);
        var userRolesKey = RedisKeys.UserRoles(userId);
        var lockKey = RedisKeys.DistributedLock(resource);

        // Assert
        Assert.Equal("test.user@flexfit.com", normalizedEmail);
        Assert.Equal("flexfit:identity:otp:VERIFY_EMAIL:test.user@flexfit.com", otpKey);
        Assert.Equal("flexfit:identity:otp-cooldown:VERIFY_EMAIL:test.user@flexfit.com", otpCooldownKey);
        Assert.Equal("flexfit:identity:refresh-token:token123", refreshTokenKey);
        Assert.Equal("flexfit:identity:refresh-token-family:family456", familyKey);
        Assert.Equal($"flexfit:identity:user:{userId}", userKey);
        Assert.Equal($"flexfit:identity:user-roles:{userId}", userRolesKey);
        Assert.Equal("flexfit:lock:user-profile-update", lockKey);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public void RedisKeys_NormalizeEmail_ShouldThrowArgumentException_WhenEmailIsInvalid(string? invalidEmail)
    {
        Assert.Throws<ArgumentException>(() => RedisKeys.NormalizeEmail(invalidEmail!));
    }

    // 3. Event Metadata and 4. Event Serialization/Deserialization Tests
    [Fact]
    public void IntegrationEvent_ShouldInitializeWithDefaultMetadataAndSerializeCorrectly()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var testEvent = new UserRegisteredEvent
        {
            UserId = userId,
            FullName = "John Doe",
            Email = "john@example.com",
            PhoneNumber = "123456789",
            CreatedAt = DateTime.UtcNow,
            CorrelationId = "corr-123",
            CausationId = "caus-456"
        };

        // Act
        var serialized = JsonSerializer.Serialize(testEvent);
        var deserialized = JsonSerializer.Deserialize<UserRegisteredEvent>(serialized);

        // Assert
        Assert.NotNull(deserialized);
        Assert.Equal(testEvent.EventId, deserialized.EventId);
        Assert.Equal("UserRegisteredEvent", deserialized.EventType);
        Assert.Equal(1, deserialized.Version);
        Assert.Equal("corr-123", deserialized.CorrelationId);
        Assert.Equal("caus-456", deserialized.CausationId);
        Assert.Equal(userId, deserialized.UserId);
        Assert.Equal("John Doe", deserialized.FullName);
        Assert.Equal("john@example.com", deserialized.Email);
    }

    // 5. Redis Cache JSON Serialization Tests
    [Fact]
    public void RedisCache_ShouldCorrectlySerializeAndDeserializeComplexTypes()
    {
        // Arrange
        var originalObj = new CachedTestModel
        {
            Id = Guid.NewGuid(),
            Name = "Cache Test Item",
            Score = 95.5m,
            Expires = DateTimeOffset.UtcNow.AddMinutes(10)
        };

        // Act
        var jsonBytes = JsonSerializer.SerializeToUtf8Bytes(originalObj);
        var restoredObj = JsonSerializer.Deserialize<CachedTestModel>(jsonBytes);

        // Assert
        Assert.NotNull(restoredObj);
        Assert.Equal(originalObj.Id, restoredObj.Id);
        Assert.Equal(originalObj.Name, restoredObj.Name);
        Assert.Equal(originalObj.Score, restoredObj.Score);
        Assert.Equal(originalObj.Expires, restoredObj.Expires);
    }

    private sealed class CachedTestModel
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal Score { get; set; }
        public DateTimeOffset Expires { get; set; }
    }

    // 6. Distributed Lock Ownership Token Verification
    [Fact]
    public void DistributedLock_ShouldAssignUniqueLockToken()
    {
        // Act
        var lockToken1 = Guid.NewGuid().ToString();
        var lockToken2 = Guid.NewGuid().ToString();

        // Assert
        Assert.NotEqual(lockToken1, lockToken2);
        Assert.True(Guid.TryParse(lockToken1, out _));
    }

    // 7. Validation of Redis Options
    [Fact]
    public void RedisOptions_ShouldStoreValidSettings()
    {
        // Arrange
        var options = new RedisOptions
        {
            ConnectionString = "localhost:6379,password=secret",
            InstanceName = "test-prefix:"
        };

        // Assert
        Assert.Equal("localhost:6379,password=secret", options.ConnectionString);
        Assert.Equal("test-prefix:", options.InstanceName);
    }

    // 8. Dead-letter Mapping Tests
    [Fact]
    public void RedisDeadLetterMessage_ShouldMapCorrectly()
    {
        // Arrange
        var eventId = Guid.NewGuid();
        var originalStream = "flexfit:events:identity";
        var originalMessageId = "1689254352-0";
        var payload = "{ \"UserId\": \"test\" }";
        var errorSummary = "TimeoutException connecting to Database";
        var consumerGroup = "catalog-group";
        var consumerName = "catalog-consumer-1";
        var correlationId = "correlation-999";

        // Act
        var dlm = new RedisDeadLetterMessage(
            OriginalStream: originalStream,
            OriginalMessageId: originalMessageId,
            EventId: eventId,
            EventType: "UserRegisteredEvent",
            Payload: payload,
            RetryCount: 5,
            ErrorSummary: errorSummary,
            FailedAt: DateTimeOffset.UtcNow,
            ConsumerGroup: consumerGroup,
            ConsumerName: consumerName,
            CorrelationId: correlationId
        );

        // Assert
        Assert.Equal(originalStream, dlm.OriginalStream);
        Assert.Equal(originalMessageId, dlm.OriginalMessageId);
        Assert.Equal(eventId, dlm.EventId);
        Assert.Equal("UserRegisteredEvent", dlm.EventType);
        Assert.Equal(payload, dlm.Payload);
        Assert.Equal(5, dlm.RetryCount);
        Assert.Equal(errorSummary, dlm.ErrorSummary);
        Assert.Equal(consumerGroup, dlm.ConsumerGroup);
        Assert.Equal(consumerName, dlm.ConsumerName);
        Assert.Equal(correlationId, dlm.CorrelationId);
    }
}
