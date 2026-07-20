using System;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.API.Models.Enums;

namespace FlexFit.Identity.API.Services.Interfaces;

public enum OtpValidationResult
{
    Valid,
    NotFound,
    Expired,
    Invalid,
    TooManyAttempts
}

public interface IOtpCacheService
{
    Task<string> CreateOtpAsync(string email, OtpPurpose purpose, CancellationToken cancellationToken = default);
    Task<OtpValidationResult> ValidateOtpAsync(string email, OtpPurpose purpose, string otpCode, CancellationToken cancellationToken = default);
    Task<bool> IsInCooldownAsync(string email, OtpPurpose purpose, CancellationToken cancellationToken = default);
}
