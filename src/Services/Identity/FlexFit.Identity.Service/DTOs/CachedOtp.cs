using System;

namespace FlexFit.Identity.Service.DTOs;

public sealed record CachedOtp(
    string OtpHash,
    int FailedAttempts,
    DateTimeOffset CreatedAt,
    DateTimeOffset ExpiresAt);
