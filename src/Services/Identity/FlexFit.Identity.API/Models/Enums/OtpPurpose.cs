namespace FlexFit.Identity.API.Models.Enums;

/// <summary>
/// Purpose of generating the OTP.
/// Used to keep token caching scoped to specific actions.
/// </summary>
public enum OtpPurpose
{
    VerifyEmail = 1,
    ResetPassword = 2,
    TwoFactorAuth = 3
}
