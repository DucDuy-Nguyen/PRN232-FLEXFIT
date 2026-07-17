using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using FlexFit.Caching;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Enums;

namespace FlexFit.Identity.Infrastructure.Security;

public sealed class RedisOtpCacheService : IOtpCacheService
{
    private readonly ICacheService _cache;
    private readonly IDistributedLockService _lockService;
    private readonly OtpOptions _options;
    private readonly ILogger<RedisOtpCacheService> _logger;

    public RedisOtpCacheService(
        ICacheService cache,
        IDistributedLockService lockService,
        IOptions<OtpOptions> options,
        ILogger<RedisOtpCacheService> logger)
    {
        _cache = cache ?? throw new ArgumentNullException(nameof(cache));
        _lockService = lockService ?? throw new ArgumentNullException(nameof(lockService));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        if (string.IsNullOrWhiteSpace(_options.HashingKey) || _options.HashingKey.Length < 16)
        {
            throw new InvalidOperationException("OTP HashingKey is required and must be at least 128 bits (16 characters).");
        }
    }

    public async Task<string> CreateOtpAsync(string email, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(email);
        var purposeStr = purpose.ToString();

        // 1. Check if cooldown is active
        if (await IsInCooldownAsync(normalizedEmail, purpose, cancellationToken))
        {
            throw new InvalidOperationException($"OTP resend cooldown is active for {normalizedEmail}. Please wait.");
        }

        // 2. Generate cryptographically secure 6-digit numeric OTP (100000 to 999999 inclusive)
        var otpCode = RandomNumberGenerator.GetInt32(100000, 1000000).ToString();
        var hash = ComputeHash(otpCode);

        var expiresAt = DateTimeOffset.UtcNow.AddMinutes(_options.ExpiryInMinutes);
        var cachedOtp = new CachedOtp(hash, 0, DateTimeOffset.UtcNow, expiresAt);

        // 3. Write OTP to Redis
        var otpKey = RedisKeys.EmailOtp(normalizedEmail, purposeStr);
        await _cache.SetAsync(otpKey, cachedOtp, TimeSpan.FromMinutes(_options.ExpiryInMinutes), cancellationToken);

        // 4. Write Cooldown marker to Redis
        var cooldownKey = RedisKeys.EmailOtpCooldown(normalizedEmail, purposeStr);
        await _cache.SetAsync(cooldownKey, "active", TimeSpan.FromSeconds(_options.CooldownInSeconds), cancellationToken);

        _logger.LogInformation("Generated and stored secure OTP for {Email} ({Purpose})", normalizedEmail, purposeStr);

        return otpCode;
    }

    public async Task<OtpValidationResult> ValidateOtpAsync(
        string email,
        OtpPurpose purpose,
        string otpCode,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(otpCode))
        {
            return OtpValidationResult.Invalid;
        }

        var normalizedEmail = RedisKeys.NormalizeEmail(email);
        var purposeStr = purpose.ToString();
        var otpKey = RedisKeys.EmailOtp(normalizedEmail, purposeStr);

        // Use distributed lock to prevent race conditions during verification & attempt incrementing
        var lockName = $"otp-validate:{purposeStr}:{normalizedEmail}";
        await using var resourceLock = await _lockService.TryAcquireAsync(lockName, TimeSpan.FromSeconds(10), cancellationToken);
        if (resourceLock == null)
        {
            // Lock is held by another concurrent verification attempt
            _logger.LogWarning("Concurrent validation attempt locked out for {Email}", normalizedEmail);
            return OtpValidationResult.Invalid;
        }

        var cachedOtp = await _cache.GetAsync<CachedOtp>(otpKey, cancellationToken);
        if (cachedOtp == null)
        {
            return OtpValidationResult.NotFound;
        }

        if (DateTimeOffset.UtcNow > cachedOtp.ExpiresAt)
        {
            await _cache.RemoveAsync(otpKey, cancellationToken);
            return OtpValidationResult.Expired;
        }

        var computedHash = ComputeHash(otpCode);
        
        // Fixed-time comparison to prevent timing attacks
        var isValid = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(cachedOtp.OtpHash),
            Encoding.UTF8.GetBytes(computedHash));

        if (isValid)
        {
            // Success: remove OTP so it cannot be reused
            await _cache.RemoveAsync(otpKey, cancellationToken);
            _logger.LogInformation("Successfully validated and cleared OTP for {Email} ({Purpose})", normalizedEmail, purposeStr);
            return OtpValidationResult.Valid;
        }

        // Mismatch: increment failed attempts
        var attempts = cachedOtp.FailedAttempts + 1;
        if (attempts >= _options.MaxFailedAttempts)
        {
            await _cache.RemoveAsync(otpKey, cancellationToken);
            _logger.LogWarning("OTP validation failed for {Email} ({Purpose}) — Max attempts ({Max}) exceeded. OTP revoked.", 
                normalizedEmail, purposeStr, _options.MaxFailedAttempts);
            return OtpValidationResult.TooManyAttempts;
        }

        // Save updated attempts count keeping original expiration
        var remainingTtl = cachedOtp.ExpiresAt - DateTimeOffset.UtcNow;
        if (remainingTtl > TimeSpan.Zero)
        {
            var updatedOtp = cachedOtp with { FailedAttempts = attempts };
            await _cache.SetAsync(otpKey, updatedOtp, remainingTtl, cancellationToken);
        }

        _logger.LogWarning("Invalid OTP submitted for {Email} ({Purpose}). Attempts remaining: {AttemptsRemaining}", 
            normalizedEmail, purposeStr, _options.MaxFailedAttempts - attempts);
            
        return OtpValidationResult.Invalid;
    }

    public async Task<bool> IsInCooldownAsync(string email, OtpPurpose purpose, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(email);
        var cooldownKey = RedisKeys.EmailOtpCooldown(normalizedEmail, purpose.ToString());
        return await _cache.ExistsAsync(cooldownKey, cancellationToken);
    }

    private string ComputeHash(string text)
    {
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_options.HashingKey));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(text));
        return Convert.ToBase64String(hashBytes);
    }
}
