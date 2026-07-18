using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Identity.API.Services.Interfaces;

public interface IGoogleTokenValidator
{
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

public sealed record GoogleUserInfo(
    string Subject,
    string Email,
    string? FullName,
    string? AvatarUrl,
    bool EmailVerified);
