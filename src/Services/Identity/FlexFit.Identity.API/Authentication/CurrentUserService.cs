using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using FlexFit.Identity.API.Services.Interfaces;

namespace FlexFit.Identity.API.Authentication;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor ?? throw new ArgumentNullException(nameof(httpContextAccessor));
    }

    public bool IsAuthenticated => 
        _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return null;

            var claim = user.FindFirst(ClaimTypes.NameIdentifier) ?? 
                        user.FindFirst(JwtRegisteredClaimNames.Sub);

            if (claim != null && Guid.TryParse(claim.Value, out var guid))
            {
                return guid;
            }

            return null;
        }
    }

    public string? Email => 
        _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.Email)?.Value ??
        _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Email)?.Value;

    public string? JwtId => 
        _httpContextAccessor.HttpContext?.User?.FindFirst(JwtRegisteredClaimNames.Jti)?.Value;

    public IReadOnlyCollection<string> Roles
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            if (user == null) return Array.Empty<string>();

            return user.FindAll(ClaimTypes.Role)
                .Select(c => c.Value)
                .ToList();
        }
    }
}
