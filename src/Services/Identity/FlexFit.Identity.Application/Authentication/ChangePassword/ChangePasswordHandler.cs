using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.Logging;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Application.Authentication.ChangePassword;

public sealed class ChangePasswordHandler : IRequestHandler<ChangePasswordCommand, ChangePasswordResponse>
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IRefreshTokenCacheService _refreshTokenCache;
    private readonly IJwtService _jwtService;
    private readonly ITokenBlacklistService _blacklistService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ChangePasswordHandler> _logger;

    public ChangePasswordHandler(
        IUserRepository userRepository,
        IPasswordHasher passwordHasher,
        IRefreshTokenCacheService refreshTokenCache,
        IJwtService jwtService,
        ITokenBlacklistService blacklistService,
        IUnitOfWork unitOfWork,
        ILogger<ChangePasswordHandler> logger)
    {
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHasher = passwordHasher ?? throw new ArgumentNullException(nameof(passwordHasher));
        _refreshTokenCache = refreshTokenCache ?? throw new ArgumentNullException(nameof(refreshTokenCache));
        _jwtService = jwtService ?? throw new ArgumentNullException(nameof(jwtService));
        _blacklistService = blacklistService ?? throw new ArgumentNullException(nameof(blacklistService));
        _unitOfWork = unitOfWork ?? throw new ArgumentNullException(nameof(unitOfWork));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<ChangePasswordResponse> Handle(ChangePasswordCommand request, CancellationToken cancellationToken)
    {
        // 1. Fetch User
        var user = await _userRepository.GetByIdAsync(request.UserId, cancellationToken);
        if (user == null || !user.IsActive)
        {
            throw new InvalidOperationException("User account not found or is inactive.");
        }

        // 2. Verify current password
        var isPasswordValid = _passwordHasher.Verify(request.CurrentPassword, user.PasswordHash);
        if (!isPasswordValid)
        {
            throw new InvalidOperationException("Incorrect current password.");
        }

        // 3. Hash and set new password
        var newHash = _passwordHasher.Hash(request.NewPassword);
        user.SetPasswordHash(newHash);
        await _userRepository.UpdateAsync(user, cancellationToken);

        // 4. Save DB changes
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        // 5. Revoke all refresh token sessions globally
        try
        {
            await _refreshTokenCache.RevokeUserSessionsAsync(user.UserId, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to revoke user sessions during password change for user {UserId}", user.UserId);
        }

        // 6. Blacklist the current JWT Access Token
        if (!string.IsNullOrWhiteSpace(request.CurrentAccessToken))
        {
            try
            {
                var principal = _jwtService.ValidateExpiredToken(request.CurrentAccessToken);
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

        return new ChangePasswordResponse("Password has been changed successfully. Other active sessions have been signed out.");
    }
}
