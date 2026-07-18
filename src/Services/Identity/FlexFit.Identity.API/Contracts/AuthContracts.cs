using System;
using FlexFit.Identity.API.Models.Enums;

namespace FlexFit.Identity.API.Contracts.Authentication;

public sealed record RegisterRequest(
    string FullName,
    string Email,
    string Password,
    string ConfirmPassword,
    string? PhoneNumber = null);

public sealed record LoginRequest(
    string Email,
    string Password);

public sealed record RefreshTokenRequest(
    string AccessToken,
    string RefreshToken);

public sealed record VerifyEmailRequest(
    string Email,
    string Otp);

public sealed record ResendOtpRequest(
    string Email,
    OtpPurpose Purpose);

public sealed record ForgotPasswordRequest(
    string Email);

public sealed record ResetPasswordRequest(
    string Email,
    string Otp,
    string Password,
    string ConfirmPassword);

public sealed record ChangePasswordRequest(
    string CurrentPassword,
    string NewPassword);
