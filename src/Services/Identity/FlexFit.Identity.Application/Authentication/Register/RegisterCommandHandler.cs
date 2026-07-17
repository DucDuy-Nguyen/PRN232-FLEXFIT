using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Entities;
using FlexFit.Identity.Domain.Enums;

namespace FlexFit.Identity.Application.Authentication.Register;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, RegisterResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpCacheService _otpCache;
    private readonly IEmailService _emailService;
    private readonly IRedisEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IMemberProfileRepository profileRepository,
        IPasswordHasher passwordHasher,
        IOtpCacheService otpCache,
        IEmailService emailService,
        IRedisEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<RegisterCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository = roleRepository ?? throw new ArgumentNullException(nameof(roleRepository));
        _profileRepository = profileRepository ?? throw new ArgumentNullException(nameof(profileRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _otpCache = otpCache ?? throw new ArgumentNullException(nameof(otpCache));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
        _eventPublisher = eventPublisher ?? throw new ArgumentNullException(nameof(eventPublisher));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RegisterResponse> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(request.Email);

        // 1. Check if email exists
        if (await _userRepository.ExistsByEmailAsync(normalizedEmail, cancellationToken))
        {
            _logger.LogWarning("Register failed: Email {Email} already exists.", normalizedEmail);
            throw new InvalidOperationException("Email is already registered.");
        }

        // 2. Hash password
        var passwordHash = _passwordHasher.Hash(request.Password);

        // 3. Create user entity
        var user = User.Create(request.FullName, normalizedEmail, passwordHash, null);

        // 4. Assign Member role
        var memberRole = await _roleRepository.GetByNameAsync("Member", cancellationToken);
        if (memberRole == null)
        {
            throw new InvalidOperationException("Member role not configured in system database.");
        }
        var userRole = UserRole.Create(user.UserId, memberRole.RoleId);
        user.AddUserRole(userRole);

        // 5. Create MemberProfile entity
        var profile = MemberProfile.Create(user.UserId);

        // 6. Save entities to repository tracking state
        await _userRepository.AddAsync(user, cancellationToken);
        await _profileRepository.AddAsync(profile, cancellationToken);

        // 7. Flush and persist database transaction before running Redis/Email operations
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 8. Generate and store OTP in Redis (sole storage)
        string otpCode;
        try
        {
            otpCode = await _otpCache.CreateOtpAsync(normalizedEmail, OtpPurpose.VerifyEmail, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Database transaction succeeded but failed to write OTP to Redis for {Email}", normalizedEmail);
            throw;
        }

        // 9. Send verification email (Smtp send failures will not roll back SQL, which is intended)
        try
        {
            await _emailService.SendOtpEmailAsync(
                toEmail: normalizedEmail,
                recipientName: user.FullName,
                otpCode: otpCode,
                purpose: "EmailVerification",
                cancellationToken: cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to deliver verification email to {Email}", normalizedEmail);
        }

        // 10. Publish Integration Event using Redis Stream
        try
        {
            var userRegisteredEvent = new UserRegisteredEvent
            {
                UserId = user.UserId,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = user.CreatedAt
            };
            
            await _eventPublisher.PublishAsync(RedisStreams.IdentityEvents, userRegisteredEvent);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish UserRegisteredEvent for {UserId}", user.UserId);
        }

        return new RegisterResponse(user.UserId, user.Email, "Registration successful. Verification email sent.");
    }
}
