using System.ComponentModel.DataAnnotations;

namespace FlexFit.Identity.Service.Configurations;

public sealed class LoginSecurityOptions
{
    public const string SectionName = "LoginSecurity";

    [Range(1, 20, ErrorMessage = "Max failed login attempts must be between 1 and 20.")]
    public int MaxFailedAttempts { get; init; } = 5;

    [Range(1, 1440, ErrorMessage = "Lockout duration must be positive and less than 1 day.")]
    public int LockoutDurationInMinutes { get; init; } = 15;
}
