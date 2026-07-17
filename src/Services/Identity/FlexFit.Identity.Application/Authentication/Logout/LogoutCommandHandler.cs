using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Application.Authentication.Logout;

public sealed class LogoutCommandHandler : IRequestHandler<LogoutCommand, LogoutResponse>
{
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly IJwtService _jwtService;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly ILogger<LogoutCommandHandler> _logger;

    public LogoutCommandHandler(
        IRefreshTokenCacheService refreshTokenCache,
        IJwtService jwtService,
        ITokenBlacklistService blacklistService,
        ILogger<LogoutCommandHandler> logger)
    {
        _refreshTokenCache = refreshTokenCache ?? throw new ArgumentNullException(nameof(refreshTokenCache));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _blacklistService = blacklistService ?? throw new ArgumentNullException(nameof(blacklistService));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<LogoutResponse> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        // 1. Blacklist current JWT Access Token
        if (!string.IsNullOrWhiteSpace(request.AccessToken))
        {
            try
            {
                var principal = _jwtService.ValidateExpiredToken(request.AccessToken);
                if (principal != null)
                {
                    var jti = principal.FindFirst(JwtRegisteredClaimNames.Jti)?.Value 
                              ?? principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var expClaim = principal.FindFirst(JwtRegisteredClaimNames.Exp)?.Value;

                    if (!string.IsNullOrEmpty(jti) && long.TryParse(expClaim, out var expUnix))
                    {
                        var expiresAt = DateTimeOffset.FromUnixTimeSeconds(expUnix);
                        await _blacklistService.BlacklistAsync(jti, expiresAt, cancellationToken);
                        _logger.LogInformation("Blacklisted access token {Jti} during logout.", jti);
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to blacklist access token during logout.");
            }
        }

        // 2. Revoke Refresh Token
        if (!string.IsNullOrWhiteSpace(request.RefreshToken))
        {
            try
            {
                var parts = request.RefreshToken.Split('.');
                if (parts.Length == 2)
                {
                    var tokenId = parts[0];
                    await _refreshTokenCache.RevokeAsync(tokenId, cancellationToken);
                    _logger.LogInformation("Revoked refresh token session {TokenId} during logout.", tokenId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to revoke refresh token during logout.");
            }
        }

        return new LogoutResponse("Logout successful.");
    }
}
