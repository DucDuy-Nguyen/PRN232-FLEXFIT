using System.ComponentModel.DataAnnotations;

namespace FlexFit.Identity.Service.Configurations;

public sealed class RefreshTokenOptions
{
    public const string SectionName = "RefreshToken";

    [Range(1, 365, ErrorMessage = "Refresh token lifetime must be between 1 and 365 days.")]
    public int ExpiryInDays { get; init; } = 30;
}
