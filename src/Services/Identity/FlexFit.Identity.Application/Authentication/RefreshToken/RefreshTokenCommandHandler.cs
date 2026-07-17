using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Application.Authentication.RefreshToken;

public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, RefreshTokenResponse>
{
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly IUserRepository _userRepository;
    private readonly IJwtService _jwtService;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenCacheService refreshTokenCache,
        IUserRepository userRepository,
        IJwtService jwtService,
        ITokenBlacklistService blacklistService,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokenCache = refreshTokenCache ?? throw new ArgumentNullException(nameof(refreshTokenCache));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _blacklistService = blacklistService ?? throw new ArgumentNullException(nameof(blacklistService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<RefreshTokenResponse> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        // 1. Validate the refresh token secret and retrieve session metadata
        // ValidateAsync will check signature, expiry, and reuse.
        // It throws exceptions on failure which map to HTTP status codes downstream.
        var validatedTokenInfo = await _refreshTokenCache.ValidateAsync(request.RefreshToken, cancellationToken);

        // 2. Load User and Role memberships
        var user = await _userRepository.GetByIdAsync(validatedTokenInfo.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new InvalidOperationException("User session is inactive or invalid.");
        }

        // 3. Rotate refresh token (Revokes current token, creates a new one in the same family)
        var rotationResult = await _refreshTokenCache.RotateAsync(request.RefreshToken, cancellationToken);

        // 4. Blacklist the old expired Access Token to prevent replaying
        try
        {
            var principal = _jwtService.ValidateExpiredToken(request.ExpiredAccessToken);
            if (principal != null)
            {
                var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value 
                          ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value; // fallback to nameid if no jti claim type resolved
                
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
            // Fail gracefully if expired access token cannot be decoded/blacklisted (log and continue)
            _logger.LogWarning(ex, "Failed to blacklist expired access token during refresh flow. Continuing execution.");
        }

        // 5. Generate new Access Token
        var roles = user.UserRoles.Select(ur => ur.Role.RoleName).ToList();
        var newAccessTokenResult = await _jwtService.GenerateAccessTokenAsync(user, roles, cancellationToken);

        _logger.LogInformation("Successfully rotated refresh token for user {UserId}", user.UserId);

        return new RefreshTokenResponse(newAccessTokenResult.Token, rotationResult.RawToken);
    }
}
