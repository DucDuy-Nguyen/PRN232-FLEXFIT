namespace FlexFit.Identity.Application.Authentication.GoogleLogin;

/// <summary>
/// Returned after a successful Google login.
/// Structure mirrors LoginResponse for consistency.
/// </summary>
public sealed record GoogleLoginResponse(
    string AccessToken,
    string RefreshToken,
    string TokenType = "Bearer",
    bool IsNewUser = false);
