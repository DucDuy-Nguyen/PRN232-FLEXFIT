using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Enums;

namespace FlexFit.Identity.Application.Authentication.ResetPassword;

public sealed class ResetPasswordHandler : IRequestHandler<ResetPasswordCommand, ResetPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpCacheService _otpCache;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ResetPasswordHandler> _logger;

    public ResetPasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IOtpCacheService otpCache,
        IRefreshTokenCacheService refreshTokenCache,
        IUnitOfWork unitOfWork,
        ILogger<ResetPasswordHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _otpCache = otpCache ?? throw new ArgumentNullException(nameof(otpCache));
        _refreshTokenCache = refreshTokenCache ?? throw new ArgumentNullException(nameof(refreshTokenCache));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResetPasswordResponse> Handle(ResetPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(request.Email);

        // 1. Validate the OTP code
        var validationResult = await _otpCache.ValidateOtpAsync(
            normalizedEmail, 
            OtpPurpose.ForgotPassword, 
            request.OtpCode, 
            cancellationToken);

        switch (validationResult)
        {
            case OtpValidationResult.NotFound:
            case OtpValidationResult.Expired:
                throw new InvalidOperationException("OTP has expired or does not exist. Please request a new reset code.");
            
            case OtpValidationResult.TooManyAttempts:
                throw new InvalidOperationException("Maximum validation attempts exceeded. OTP revoked. Please request a new reset code.");

            case OtpValidationResult.Invalid:
                throw new InvalidOperationException("Invalid OTP code submitted.");

            case OtpValidationResult.Valid:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        // 2. Load User
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new InvalidOperationException("User account not found or is inactive.");
        }

        // 3. Hash and set new password
        var newHash = _passwordHasher.Hash(request.Password);
        user.SetPasswordHash(newHash);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 4. Save DB changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Revoke all active refresh token sessions to force logout on all devices
        try
        {
            await _refreshTokenCache.RevokeUserSessionsAsync(user.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke user sessions during password reset for user {UserId}", user.UserId);
        }

        _logger.LogInformation("Password reset successfully for email {Email}", normalizedEmail);

        return new ResetPasswordResponse("Password has been reset successfully. Please log in with your new password.");
    }
}
