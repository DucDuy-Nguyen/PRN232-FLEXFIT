using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Caching;
using FlexFit.Identity.Application.Abstractions;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Application.Authentication.Login;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, LoginResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IJwtService _jwtService;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly ILoginAttemptService _loginAttemptService;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IJwtService jwtService,
        IRefreshTokenCacheService refreshTokenCache,
        ILoginAttemptService loginAttemptService,
        ILogger<LoginCommandHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _refreshTokenCache = refreshTokenCache ?? throw new ArgumentNullException(nameof(refreshTokenCache));
        _loginAttemptService = loginAttemptService ?? throw new ArgumentNullException(nameof(loginAttemptService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LoginResponse> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = RedisKeys.NormalizeEmail(request.Email);

        // 1. Check if email is currently locked out
        if (await _loginAttemptService.IsBlockedAsync(normalizedEmail, cancellationToken))
        {
            _logger.LogWarning("Login blocked: Account {Email} is currently locked out.", normalizedEmail);
            throw new InvalidOperationException("Account is locked out due to multiple failed attempts. Please try again later.");
        }

        // 2. Find User
        var user = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (user == null)
        {
            // Record failure atomically in Redis (but throw generic credential error to avoid enumeration)
            await _loginAttemptService.RecordFailureAsync(normalizedEmail, cancellationToken);
            throw new InvalidOperationException("Invalid email or password.");
        }

        // 3. Verify Password
        var isPasswordValid = _passwordHasher.Verify(request.Password, user.PasswordHash);
        if (!isPasswordValid)
        {
            await _loginAttemptService.RecordFailureAsync(normalizedEmail, cancellationToken);
            throw new InvalidOperationException("Invalid email or password.");
        }

        // 4. Check Active
        if (!user.IsActive)
        {
            throw new InvalidOperationException("Account is deactivated.");
        }

        // 5. Check Email Verified
        if (!user.IsEmailVerified)
        {
            throw new InvalidOperationException("Email is not verified. Please verify your email first.");
        }

        // 6. Generate JWT Access Token
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var accessTokenResult = await _jwtService.GenerateAccessTokenAsync(user, roles, cancellationToken);

        // 7. Generate Refresh Token
        var refreshTokenResult = await _refreshTokenCache.CreateAsync(user.UserId, cancellationToken);

        // 8. Reset login failure attempts counter
        await _loginAttemptService.ResetAsync(normalizedEmail, cancellationToken);

        _logger.LogInformation("User {Email} logged in successfully.", normalizedEmail);

        return new LoginResponse(accessTokenResult.Token, refreshTokenResult.RawToken);
    }
}
