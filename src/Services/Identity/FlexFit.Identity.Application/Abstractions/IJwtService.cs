using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using FlexFit.Identity.Domain.Entities;

namespace FlexFit.Identity.Application.Abstractions;

public interface IJwtService
{
    Task<AccessTokenResult> GenerateAccessTokenAsync(
        User user,
        IReadOnlyCollection<string> roles,
        CancellationToken cancellationToken = default);

    ClaimsPrincipal? ValidateExpiredToken(
        string token);
}

public sealed record AccessTokenResult(
    string Token,
    string JwtId,
    DateTimeOffset ExpiresAt);
