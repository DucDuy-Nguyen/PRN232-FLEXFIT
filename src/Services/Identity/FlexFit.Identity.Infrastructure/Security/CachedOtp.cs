using System;

namespace FlexFit.Identity.Infrastructure.Security;

public sealed record CachedOtp(
    string OtpHash,
    int FailedAttempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
