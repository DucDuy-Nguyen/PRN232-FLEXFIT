using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Identity.Application.Abstractions;

/// <summary>
/// Validates a Google ID token and extracts verified user information.
/// Implementation lives in Infrastructure; Application has no direct dependency on Google SDK.
/// </summary>
public interface IGoogleTokenValidator
{
    /// <summary>
    /// Validates the Google ID token signature, issuer, audience, and expiry.
    /// Returns null if the token is invalid or cannot be validated.
    /// </summary>
    Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default);
}

/// <summary>
/// Verified user information extracted from a valid Google ID token.
/// All fields come from the signed token payload — client cannot forge them.
/// </summary>
public sealed record GoogleUserInfo(
    string Subject,       // Google "sub" claim — unique and stable per Google account
    string Email,
    string? FullName,
    string? AvatarUrl,
    bool EmailVerified);  // Must be true before allowing login
