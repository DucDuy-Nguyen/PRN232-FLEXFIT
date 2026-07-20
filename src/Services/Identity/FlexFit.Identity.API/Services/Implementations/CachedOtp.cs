using System;

namespace FlexFit.Identity.API.Services.Implementations;

public sealed record CachedOtp(
    string OtpHash,
    int FailedAttempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
