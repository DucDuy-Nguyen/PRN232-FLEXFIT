using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Enums;

namespace FlexFit.Identity.Application.Authentication.ResendOtp;

public sealed class ResendOtpHandler : IRequestHandler<ResendOtpCommand, ResendOtpResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IOtpCacheService _otpCache;
    private readonly IEmailService _emailService;
    private readonly ILogger<ResendOtpHandler> _logger;

    public ResendOtpHandler(
        IUserRepository userRepository,
        IOtpCacheService otpCache,
        IEmailService emailService,
        ILogger<ResendOtpHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _otpCache = otpCache ?? throw new ArgumentNullException(nameof(otpCache));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ResendOtpResponse> Handle(ResendOtpCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(request.Email);

        // 1. Verify user exists and is active
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new InvalidOperationException("User account not found or is inactive.");
        }

        // 2. Check if OTP is in cooldown
        var isInCooldown = await _otpCache.IsInCooldownAsync(normalizedEmail, request.Purpose, cancellationToken);
        if (isInCooldown)
        {
            throw new InvalidOperationException("Resend request is in cooldown. Please wait before trying again.");
        }

        // 3. Generate and store OTP in Redis
        var purposeStr = request.Purpose.ToString();
        string otpCode;
        try
        {
            otpCode = await _otpCache.CreateOtpAsync(normalizedEmail, request.Purpose, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to write resent OTP to Redis for {Email}", normalizedEmail);
            throw;
        }

        // 4. Send email
        try
        {
            await _emailService.SendOtpEmailAsync(
                toEmail: normalizedEmail,
                recipientName: user.FullName,
                otpCode: otpCode,
                purpose: purposeStr,
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send resent OTP email to {Email}", normalizedEmail);
        }

        return new ResendOtpResponse("A new OTP code has been sent to your email.");
    }
}
