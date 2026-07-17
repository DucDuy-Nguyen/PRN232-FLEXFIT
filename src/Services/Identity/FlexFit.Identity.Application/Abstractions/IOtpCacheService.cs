using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Domain.Enums;

namespace FlexFit.Identity.Application.Abstractions;

public enum OtpValidationResult
{
    Valid,
    NotFound,
    Expired,
    Invalid,
    TooManyAttempts
}

public interface IOtpCacheService
{
    /// <summary>
    /// Generates a secure OTP, hashes it, and stores the hash in Redis with TTL.
    /// Returns the plaintext OTP to be sent via email.
    /// </summary>
    Task<string> CreateOtpAsync(string email, OtpPurpose purpose, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates a submitted OTP against the stored hash.
    /// Increments FailedAttempts on mismatch.
    /// Deletes the OTP key on success or on max attempts limit.
    /// Returns OtpValidationResult.
    /// </summary>
    Task<OtpValidationResult> ValidateOtpAsync(string email, OtpPurpose purpose, string otpCode, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks whether a cooldown is active for the given email+purpose combination.
    /// </summary>
    Task<bool> IsInCooldownAsync(string email, OtpPurpose purpose, CancellationToken cancellationToken = default);
}
