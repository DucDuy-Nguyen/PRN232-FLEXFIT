namespace FlexFit.Identity.Domain.Enums;

/// <summary>
/// Represents the purpose of an OTP request.
/// Used as part of the Redis key convention:
///   flexfit:identity:otp:{Purpose}:{normalizedEmail}
/// Must match the "reason" parameter in ResendOtpRequest.
/// </summary>
public enum OtpPurpose
{
    VerifyEmail,
    ForgotPassword
}
