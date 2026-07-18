using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Identity.API.Services.Interfaces;

public interface ITokenBlacklistService
{
    Task BlacklistAsync(
        string jwtId,
        DateTimeOffset expiresAt,
        CancellationToken cancellationToken = default);

    Task<bool> IsBlacklistedAsync(
        string jwtId,
        CancellationToken cancellationToken = default);
}
