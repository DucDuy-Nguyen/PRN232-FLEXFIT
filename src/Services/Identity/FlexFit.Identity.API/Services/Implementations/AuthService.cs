using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using FlexFit.Identity.API.Contracts.Authentication;
using FlexFit.Identity.API.Services.Interfaces;
using FlexFit.Identity.API.Services.Interfaces;
using FlexFit.Identity.API.Models.Entities;
using FlexFit.Identity.API.Models.Enums;
using FlexFit.Identity.API.Models.Exceptions;
using FlexFit.RedisEventBus;
using FlexFit.Contracts;
using FlexFit.Identity.API.Data.Repositories.Interfaces;

namespace FlexFit.Identity.API.Services.Implementations;

public sealed class AuthService : IAuthService
{
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IOtpCacheService _otpCache;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly ILoginAttemptService _loginAttempt;
    private readonly IEmailService _emailService;
    private readonly IJwtService _jwtService;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly IRedisEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly ILogger<AuthService> _logger;

    public AuthService(
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IMemberProfileRepository profileRepository,
        IPasswordHasher passwordHasher,
        IOtpCacheService otpCache,
        IRefreshTokenCacheService refreshTokenCache,
        ILoginAttemptService loginAttempt,
        IEmailService emailService,
        IJwtService jwtService,
        ITokenBlacklistService blacklistService,
        IRedisEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        IGoogleTokenValidator googleTokenValidator,
        ILogger<AuthService> logger)
    {
        _userRepository = userRepository;
        _roleRepository = roleRepository;
        _profileRepository = profileRepository;
        _passwordHasher = passwordHasher;
        _otpCache = otpCache;
        _refreshTokenCache = refreshTokenCache;
        _loginAttempt = loginAttempt;
        _emailService = emailService;
        _jwtService = jwtService;
        _blacklistService = blacklistService;
        _eventPublisher = eventPublisher;
        _unitOfWork = unitOfWork;
        _googleTokenValidator = googleTokenValidator;
        _logger = logger;
    }

    public async Task<RegisterResponse> RegisterAsync(
        string fullName, 
        string email, 
        string password, 
        string? phoneNumber, 
        CancellationToken cancellationToken = default)
    {
        if (await _userRepository.ExistsByEmailAsync(email, cancellationToken))
        {
            throw new EmailAlreadyExistsException(email);
        }

        var passwordHash = _passwordHasher.Hash(password);
        var user = User.Create(fullName, email, passwordHash, phoneNumber);

        var memberRole = await _roleRepository.GetByNameAsync("Member", cancellationToken);
        if (memberRole == null)
        {
            throw new KeyNotFoundException("Default role 'Member' was not found in the database.");
        }

        user.AddUserRole(UserRole.Create(user.UserId, memberRole.RoleId));

        var profile = MemberProfile.Create(user.UserId);

        await _userRepository.AddAsync(user, cancellationToken);
        await _profileRepository.AddAsync(profile, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("Successfully registered user {Email} with ID {UserId}", email, user.UserId);

        // Publish registration event
        await _eventPublisher.PublishAsync("identity-events", new UserRegisteredEvent
        {
            UserId = user.UserId,
            Email = user.Email,
            FullName = user.FullName,
            PhoneNumber = user.PhoneNumber,
            CreatedAt = DateTime.UtcNow
        });

        // Generate and send verification email
        try
        {
            var otpCode = await _otpCache.CreateOtpAsync(email, OtpPurpose.VerifyEmail, cancellationToken);
            await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otpCode, "VerifyEmail", cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Registration succeeded but failed to send OTP to {Email}", email);
        }

        return new RegisterResponse(user.UserId, user.Email, "User registered successfully. Verification email sent.");
    }

    public async Task<LoginResponse> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
    {
        var normalizedEmail = email.Trim().ToLowerInvariant();

        if (await _loginAttempt.IsBlockedAsync(normalizedEmail, cancellationToken))
        {
            throw new AccountNotActiveException();
        }

        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null || !_passwordHasher.Verify(password, user.PasswordHash))
        {
            var attempt = await _loginAttempt.RecordFailureAsync(normalizedEmail, cancellationToken);
            if (attempt.IsBlocked)
            {
                throw new AccountNotActiveException();
            }
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        if (!user.IsActive)
        {
            throw new AccountNotActiveException();
        }

        await _loginAttempt.ResetAsync(normalizedEmail, cancellationToken);

        user.RecordLogin();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        var roles = new List<string>();
        foreach (var ur in user.UserRoles)
        {
            if (ur.Role != null)
            {
                roles.Add(ur.Role.RoleName);
            }
        }

        var accessTokenResult = await _jwtService.GenerateAccessTokenAsync(user, roles, cancellationToken);
        var refreshTokenResult = await _refreshTokenCache.CreateAsync(user.UserId, cancellationToken);

        return new LoginResponse(
            accessTokenResult.Token,
            refreshTokenResult.RawToken,
            accessTokenResult.ExpiresAt
        );
    }

    public async Task<GoogleLoginResponse> GoogleLoginAsync(string idToken, CancellationToken cancellationToken = default)
    {
        var googleInfo = await _googleTokenValidator.ValidateAsync(idToken, cancellationToken);
        if (googleInfo == null)
        {
            throw new UnauthorizedAccessException("Invalid Google ID token.");
        }

        if (!googleInfo.EmailVerified)
        {
            throw new UnauthorizedAccessException("Google account email is not verified.");
        }

        var user = await _userRepository.GetByEmailAsync(googleInfo.Email, cancellationToken);
        var isNewUser = false;

        if (user == null)
        {
            isNewUser = true;
            user = User.CreateFromGoogle(googleInfo.FullName ?? "Google User", googleInfo.Email, googleInfo.Subject);

            var memberRole = await _roleRepository.GetByNameAsync("Member", cancellationToken);
            if (memberRole == null)
            {
                throw new KeyNotFoundException("Default role 'Member' was not found in the database.");
            }

            user.AddUserRole(UserRole.Create(user.UserId, memberRole.RoleId));
            var profile = MemberProfile.Create(user.UserId);

            await _userRepository.AddAsync(user, cancellationToken);
            await _profileRepository.AddAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully created new user {Email} via Google OAuth", googleInfo.Email);

            await _eventPublisher.PublishAsync("identity-events", new UserRegisteredEvent
            {
                UserId = user.UserId,
                Email = user.Email,
                FullName = user.FullName,
                PhoneNumber = user.PhoneNumber,
                CreatedAt = DateTime.UtcNow
            });

            await _eventPublisher.PublishAsync("identity-events", new EmailVerifiedEvent
            {
                UserId = user.UserId,
                Email = user.Email
            });
        }
        else
        {
            if (!user.IsActive)
            {
                throw new UnauthorizedAccessException("Account is inactive or locked.");
            }

            if (user.GoogleSubject == null)
            {
                user.SetGoogleSubject(googleInfo.Subject);
                if (!user.IsEmailVerified)
                {
                    user.MarkEmailVerified();
                }
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        var roles = new List<string>();
        foreach (var ur in user.UserRoles)
        {
            if (ur.Role != null)
            {
                roles.Add(ur.Role.RoleName);
            }
        }

        if (roles.Count == 0)
        {
            roles.Add("Member");
        }

        var accessTokenResult = await _jwtService.GenerateAccessTokenAsync(user, roles, cancellationToken);
        var refreshTokenResult = await _refreshTokenCache.CreateAsync(user.UserId, cancellationToken);

        return new GoogleLoginResponse(
            accessTokenResult.Token,
            refreshTokenResult.RawToken,
            accessTokenResult.ExpiresAt,
            isNewUser
        );
    }

    public async Task<LoginResponse> RefreshTokenAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        var principal = _jwtService.ValidateExpiredToken(accessToken);
        if (principal == null)
        {
            throw new InvalidRefreshTokenException();
        }

        var validatedInfo = await _refreshTokenCache.ValidateAsync(refreshToken, cancellationToken);
        var user = await _userRepository.GetByIdAsync(validatedInfo.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new InvalidRefreshTokenException();
        }

        var rotatedResult = await _refreshTokenCache.RotateAsync(refreshToken, cancellationToken);

        var roles = new List<string>();
        foreach (var ur in user.UserRoles)
        {
            if (ur.Role != null)
            {
                roles.Add(ur.Role.RoleName);
            }
        }

        var newAccessTokenResult = await _jwtService.GenerateAccessTokenAsync(user, roles, cancellationToken);

        return new LoginResponse(
            newAccessTokenResult.Token,
            rotatedResult.RawToken,
            newAccessTokenResult.ExpiresAt
        );
    }

    public async Task VerifyEmailAsync(string email, string otpCode, CancellationToken cancellationToken = default)
    {
        var result = await _otpCache.ValidateOtpAsync(email, OtpPurpose.VerifyEmail, otpCode, cancellationToken);
        if (result != OtpValidationResult.Valid)
        {
            throw new OtpValidationException($"OTP validation failed: {result}");
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(email);
        }

        user.MarkEmailVerified();
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _eventPublisher.PublishAsync("identity-events", new EmailVerifiedEvent
        {
            UserId = user.UserId,
            Email = user.Email
        });
    }

    public async Task ResendOtpAsync(string email, string purpose, CancellationToken cancellationToken = default)
    {
        if (!Enum.TryParse<OtpPurpose>(purpose, true, out var otpPurpose))
        {
            throw new ArgumentException("Invalid OTP purpose.", nameof(purpose));
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(email);
        }

        var otpCode = await _otpCache.CreateOtpAsync(user.Email, otpPurpose, cancellationToken);
        await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otpCode, purpose, cancellationToken);
    }

    public async Task ForgotPasswordAsync(string email, CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(email);
        }

        var otpCode = await _otpCache.CreateOtpAsync(user.Email, OtpPurpose.ResetPassword, cancellationToken);
        await _emailService.SendOtpEmailAsync(user.Email, user.FullName, otpCode, "ForgotPassword", cancellationToken);
    }

    public async Task ResetPasswordAsync(
        string email, 
        string otpCode, 
        string newPassword, 
        string confirmPassword, 
        CancellationToken cancellationToken = default)
    {
        if (newPassword != confirmPassword)
        {
            throw new ArgumentException("Passwords do not match.");
        }

        var result = await _otpCache.ValidateOtpAsync(email, OtpPurpose.ResetPassword, otpCode, cancellationToken);
        if (result != OtpValidationResult.Valid)
        {
            throw new OtpValidationException($"OTP validation failed: {result}");
        }

        var user = await _userRepository.GetByEmailAsync(email, cancellationToken);
        if (user == null)
        {
            throw new UserNotFoundException(email);
        }

        var newHash = _passwordHasher.Hash(newPassword);
        user.SetPasswordHash(newHash);

        await _refreshTokenCache.RevokeUserSessionsAsync(user.UserId, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public async Task ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        string currentAccessToken,
        CancellationToken cancellationToken = default)
    {
        var user = await _userRepository.GetByIdAsync(userId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new InvalidOperationException("User account not found or is inactive.");
        }

        var isPasswordValid = _passwordHasher.Verify(currentPassword, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new InvalidOperationException("Incorrect current password.");
        }

        var newHash = _passwordHasher.Hash(newPassword);
        user.SetPasswordHash(newHash);
        await _userRepository.UpdateAsync(user, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        try
        {
            await _refreshTokenCache.RevokeUserSessionsAsync(user.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke user sessions during password change for user {UserId}", user.UserId);
        }

        if (!string.IsNullOrWhiteSpace(currentAccessToken))
        {
            try
            {
                var principal = _jwtService.ValidateExpiredToken(currentAccessToken);
                if (principal != null)
                {
                    var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value 
                              ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

                    if (!string.IsNullOrEmpty(jti) && long.TryParse(expClaim, out var expUnix))
                    {
                        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);
                        await _blacklistService.BlacklistAsync(jti, expiresAt, cancellationToken);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to blacklist current access token during password change.");
            }
        }

        _logger.LogInformation("Password successfully changed for user {UserId}", user.UserId);
    }

    public async Task LogoutAsync(string accessToken, string refreshToken, CancellationToken cancellationToken = default)
    {
        var principal = _jwtService.ValidateExpiredToken(accessToken);
        if (principal == null)
        {
            throw new SecurityTokenException("Invalid token.");
        }

        var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;
        var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

        if (string.IsNullOrEmpty(jti) || string.IsNullOrEmpty(expClaim) || !long.TryParse(expClaim, out var expUnix))
        {
            throw new SecurityTokenException("Invalid token metadata.");
        }

        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);

        await _blacklistService.BlacklistAsync(jti, expiresAt, cancellationToken);

        var parts = refreshToken.Split('.');
        if (parts.Length == 2)
        {
            await _refreshTokenCache.RevokeAsync(parts[0], cancellationToken);
        }
    }
}
