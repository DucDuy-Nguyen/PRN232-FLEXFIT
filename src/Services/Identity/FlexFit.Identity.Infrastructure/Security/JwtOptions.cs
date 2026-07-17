using System;
using System.ComponentModel.DataAnnotations;

namespace FlexFit.Identity.Infrastructure.Security;

public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    [Required]
    [MinLength(32, ErrorMessage = "JWT Key must be at least 256 bits (32 characters) long.")]
    public string Key { get; init; } = string.Empty;

    [Required]
    public string Issuer { get; init; } = string.Empty;

    [Required]
    public string Audience { get; init; } = string.Empty;

    [Range(1, 1440, ErrorMessage = "Token lifetime must be positive and less than 1 day.")]
    public int ExpiresInMinutes { get; init; } = 60;
}
