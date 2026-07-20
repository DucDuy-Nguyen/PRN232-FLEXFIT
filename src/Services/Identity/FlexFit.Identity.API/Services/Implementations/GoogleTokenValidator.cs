using System;
using System.Threading;
using System.Threading.Tasks;
using Google.Apis.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using FlexFit.Identity.API.Services.Interfaces;

namespace FlexFit.Identity.API.Services.Implementations;

public sealed class GoogleTokenValidator : IGoogleTokenValidator
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<GoogleTokenValidator> _logger;

    public GoogleTokenValidator(IConfiguration configuration, ILogger<GoogleTokenValidator> logger)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(idToken))
        {
            return null;
        }

        try
        {
            var clientId = _configuration["GoogleAuthentication:ClientId"];
            if (string.IsNullOrWhiteSpace(clientId))
            {
                _logger.LogError("Google ClientId is not configured. Unable to validate ID token.");
                return null;
            }

            var settings = new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = new[] { clientId }
            };

            var payload = await GoogleJsonWebSignature.ValidateAsync(idToken, settings);
            if (payload == null)
            {
                return null;
            }

            return new GoogleUserInfo(
                Subject: payload.Subject,
                Email: payload.Email,
                FullName: payload.Name,
                AvatarUrl: payload.Picture,
                EmailVerified: payload.EmailVerified);
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(ex, "Failed to validate Google ID token: invalid signature, expired, or wrong audience.");
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error validating Google ID token.");
            return null;
        }
    }
}
