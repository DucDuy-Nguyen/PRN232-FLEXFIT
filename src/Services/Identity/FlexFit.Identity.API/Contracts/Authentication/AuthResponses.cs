using System;

namespace FlexFit.Identity.API.Contracts.Authentication;

/// <summary>Returned after successful Register.</summary>
public sealed record RegisterResponse(
    Guid UserId,
    string Email,
    string Message);

/// <summary>Returned after successful Login or RefreshToken.</summary>
public sealed record LoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    string TokenType = "Bearer");

/// <summary>Returned after successful Google login.</summary>
public sealed record GoogleLoginResponse(
    string AccessToken,
    string RefreshToken,
    DateTimeOffset ExpiresAt,
    bool IsNewUser = false,
    string TokenType = "Bearer");
