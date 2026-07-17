using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Contracts;
using FlexFit.RedisEventBus;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Application.Authentication.GoogleLogin;

public sealed class GoogleLoginCommandHandler : IRequestHandler<GoogleLoginCommand, GoogleLoginResponse>
{
    private readonly IGoogleTokenValidator _googleTokenValidator;
    private readonly IUserRepository _userRepository;
    private readonly IRoleRepository _roleRepository;
    private readonly IMemberProfileRepository _profileRepository;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly IRedisEventPublisher _eventPublisher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<GoogleLoginCommandHandler> _logger;

    public GoogleLoginCommandHandler(
        IGoogleTokenValidator googleTokenValidator,
        IUserRepository userRepository,
        IRoleRepository roleRepository,
        IMemberProfileRepository profileRepository,
        IJwtService jwtService,
        IRefreshTokenCacheService refreshTokenCache,
        IRedisEventPublisher eventPublisher,
        IUnitOfWork unitOfWork,
        ILogger<GoogleLoginCommandHandler> logger)
    {
        _googleTokenValidator = googleTokenValidator ?? throw new ArgumentNullException(nameof(googleTokenValidator));
        _userRepository      = userRepository      ?? throw new ArgumentNullException(nameof(userRepository));
        _roleRepository      = roleRepository      ?? throw new ArgumentNullException(nameof(roleRepository));
        _profileRepository   = profileRepository   ?? throw new ArgumentNullException(nameof(profileRepository));
        _jwtService          = jwtService          ?? throw new ArgumentNullException(nameof(jwtService));
        _refreshTokenCache   = refreshTokenCache   ?? throw new ArgumentNullException(nameof(refreshTokenCache));
        _eventPublisher      = eventPublisher      ?? throw new ArgumentNullException(nameof(eventPublisher));
        _unitOfWork          = unitOfWork          ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger              = logger              ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GoogleLoginResponse> Handle(GoogleLoginCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate Google ID token (signature, audience, expiry)
        var googleUser = await _googleTokenValidator.ValidateAsync(request.IdToken, cancellationToken);
        if (googleUser == null)
        {
            _logger.LogWarning("Google login failed: token validation returned null.");
            throw new UnauthorizedAccessException("Invalid Google ID token.");
        }

        // 2. Reject unverified emails — Google can issue tokens for unverified accounts
        if (!googleUser.EmailVerified)
        {
            _logger.LogWarning("Google login rejected: email not verified for subject {Subject}.", googleUser.Subject);
            throw new UnauthorizedAccessException("Google account email is not verified.");
        }

        var normalizedEmail = RedisKeys.NormalizeEmail(googleUser.Email);

        // 3. Look up user by email
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        bool isNewUser = false;

        if (user == null)
        {
            // --- NEW USER FLOW ---
            isNewUser = true;
            _logger.LogInformation("Google login: creating new user for email {Email}.", normalizedEmail);

            var fullName = googleUser.FullName ?? normalizedEmail;
            user = User.CreateFromGoogle(fullName, normalizedEmail, googleUser.Subject);

            // Set avatar if provided by Google
            if (!string.IsNullOrWhiteSpace(googleUser.AvatarUrl))
            {
                user.UpdateProfile(null, null, null, googleUser.AvatarUrl);
            }

            // Assign default Member role
            var memberRole = await _roleRepository.GetByNameAsync("Member", cancellationToken);
            if (memberRole == null)
            {
                throw new InvalidOperationException("Member role not configured in system database.");
            }
            var userRole = UserRole.Create(user.UserId, memberRole.RoleId);
            user.AddUserRole(userRole);

            // Create MemberProfile
            var profile = MemberProfile.Create(user.UserId);

            await _userRepository.AddAsync(user, cancellationToken);
            await _profileRepository.AddAsync(profile, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            // Publish registration events
            try
            {
                await _eventPublisher.PublishAsync(RedisStreams.IdentityEvents, new UserRegisteredEvent
                {
                    UserId = user.UserId,
                    FullName = user.FullName,
                    Email = user.Email,
                    CreatedAt = user.CreatedAt
                });

                await _eventPublisher.PublishAsync(RedisStreams.IdentityEvents, new EmailVerifiedEvent
                {
                    UserId = user.UserId,
                    Email = user.Email
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to publish registration events for new Google user {UserId}.", user.UserId);
                // Dual-write risk acknowledged; Outbox pattern deferred
            }
        }
        else
        {
            // --- EXISTING USER FLOW ---
            if (!user.IsActive)
            {
                _logger.LogWarning("Google login rejected: user {UserId} is inactive.", user.UserId);
                throw new UnauthorizedAccessException("Account is deactivated. Contact support.");
            }

            // Link GoogleSubject if not yet set (first time using Google on an existing account)
            if (user.GoogleSubject == null)
            {
                user.SetGoogleSubject(googleUser.Subject);
                await _userRepository.UpdateAsync(user, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
        }

        // 4. Generate access token
        var roles = user.UserRoles
            .Select(ur => ur.Role?.RoleName)
            .Where(name => name != null)
            .Select(name => name!)
            .ToList();

        if (roles.Count == 0)
        {
            // Fallback for in-memory tracker scenarios (like new user signup)
            roles.Add("Member");
        }

        var accessTokenResult = await _jwtService.GenerateAccessTokenAsync(user, roles, cancellationToken);

        // 5. Generate refresh token
        var refreshTokenResult = await _refreshTokenCache.CreateAsync(user.UserId, cancellationToken);

        _logger.LogInformation("Google login successful for user {UserId} (newUser={IsNewUser}).", user.UserId, isNewUser);

        return new GoogleLoginResponse(accessTokenResult.Token, refreshTokenResult.RawToken, IsNewUser: isNewUser);
    }
}
