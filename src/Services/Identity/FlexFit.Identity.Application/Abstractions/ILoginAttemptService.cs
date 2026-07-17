using System;
using System.Threading;
using System.Threading.Tasks;

namespace FlexFit.Identity.Application.Abstractions;

public sealed record LoginAttemptResult(
    int FailedAttempts,
    bool IsBlocked,
    TimeSpan? BlockedRemaining);

public interface ILoginAttemptService
{
    Task<LoginAttemptResult> RecordFailureAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task ResetAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);

    Task<bool> IsBlockedAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default);
}
