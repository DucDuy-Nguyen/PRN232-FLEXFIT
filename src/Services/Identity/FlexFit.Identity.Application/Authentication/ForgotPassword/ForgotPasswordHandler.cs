using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Enums;

namespace FlexFit.Identity.Application.Authentication.ForgotPassword;

public sealed class ForgotPasswordHandler : IRequestHandler<ForgotPasswordCommand, ForgotPasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpCacheService _otpCache;
    private readonly IEmailService _emailService;
    private readonly ILogger<ForgotPasswordHandler> _logger;

    public ForgotPasswordHandler(
        IUserRepository userRepository,
        IOtpCacheService otpCache,
        IEmailService emailService,
        ILogger<ForgotPasswordHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _otpCache = otpCache ?? throw new ArgumentNullException(nameof(otpCache));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ForgotPasswordResponse> Handle(ForgotPasswordCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(request.Email);
        var successMessage = "If the email is registered in our system, a password reset code will be delivered shortly.";

        // 1. Verify user existence
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null || !user.IsActive)
        {
            // Return success response anyway to prevent user enumeration
            _logger.LogInformation("Forgot password requested for non-existent/inactive email {Email}. Masking success response.", normalizedEmail);
            return new ForgotPasswordResponse(successMessage);
        }

        // 2. Cooldown check - if resend OTP is in cooldown, return error / wait state
        if (await _otpCache.IsInCooldownAsync(normalizedEmail, OtpPurpose.ForgotPassword, cancellationToken))
        {
            throw new InvalidOperationException("Please wait before requesting a new password reset code.");
        }

        // 3. Generate and store PasswordReset OTP in Redis
        string otpCode;
        try
        {
            otpCode = await _otpCache.CreateOtpAsync(normalizedEmail, OtpPurpose.ForgotPassword, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write password reset OTP to Redis for {Email}", normalizedEmail);
            throw;
        }

        // 4. Dispatch Email with OTP
        try
        {
            await _emailService.SendOtpEmailAsync(
                toEmail: normalizedEmail,
                recipientName: user.FullName,
                otpCode: otpCode,
                purpose: "PasswordReset",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send password reset email to {Email}", normalizedEmail);
        }

        return new ForgotPasswordResponse(successMessage);
    }
}
