using System;
using System.ComponentModel.DataAnnotations;

namespace FlexFit.Identity.Infrastructure.Security;

public sealed class OtpOptions
{
    public const string SectionName = "Otp";

    [Required]
    [MinLength(16, ErrorMessage = "OTP hashing key must be at least 128 bits (16 characters) long.")]
    public string HashingKey { get; init; } = string.Empty;

    [Range(1, 60, ErrorMessage = "OTP expiry must be between 1 and 60 minutes.")]
    public int ExpiryInMinutes { get; init; } = 5;

    [Range(10, 600, ErrorMessage = "OTP cooldown must be between 10 and 600 seconds.")]
    public int CooldownInSeconds { get; init; } = 60;

    [Range(1, 10, ErrorMessage = "Max failed attempts must be between 1 and 10.")]
    public int MaxFailedAttempts { get; init; } = 5;
}
