using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Enums;

namespace FlexFit.Identity.Application.Authentication.VerifyEmail;

public sealed class VerifyEmailHandler : IRequestHandler<VerifyEmailCommand, VerifyEmailResponse>
{
    private readonly IOtpCacheService _otpCache;
    private readonly IUserRepository _userRepository;
    private readonly IRedisEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<VerifyEmailHandler> _logger;

    public VerifyEmailHandler(
        IOtpCacheService otpCache,
        IUserRepository userRepository,
        IRedisEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<VerifyEmailHandler> logger)
    {
        _otpCache = otpCache ?? throw new ArgumentNullException(nameof(otpCache));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<VerifyEmailResponse> Handle(VerifyEmailCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(request.Email);

        // 1. Validate OTP from Redis cache (no SQL read of OTP legacy fields)
        var validationResult = await _otpCache.ValidateOtpAsync(
            normalizedEmail, 
            OtpPurpose.VerifyEmail, 
            request.OtpCode, 
            cancellationToken);

        switch (validationResult)
        {
            case OtpValidationResult.NotFound:
            case OtpValidationResult.Expired:
                throw new InvalidOperationException("OTP has expired or does not exist. Please request a new code.");
            
            case OtpValidationResult.TooManyAttempts:
                throw new InvalidOperationException("Maximum validation attempts exceeded. OTP revoked. Please request a new code.");

            case OtpValidationResult.Invalid:
                throw new InvalidOperationException("Invalid OTP code submitted.");

            case OtpValidationResult.Valid:
                break;

            default:
                throw new ArgumentOutOfRangeException();
        }

        // 2. Fetch User and update verification state
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null)
        {
            throw new InvalidOperationException("User account not found.");
        }

        user.MarkEmailVerified();
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 3. Save changes in DB
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Email address {Email} was successfully verified.", normalizedEmail);

        // 4. Publish integration event
        try
        {
            var emailVerifiedEvent = new EmailVerifiedEvent
            {
                UserId = user.UserId,
                Email = user.Email
            };
            await _eventPublisher.PublishAsync(RedisStreams.IdentityEvents, emailVerifiedEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish EmailVerifiedEvent for user {UserId}", user.UserId);
        }

        return new VerifyEmailResponse("Email verified successfully.");
    }
}
