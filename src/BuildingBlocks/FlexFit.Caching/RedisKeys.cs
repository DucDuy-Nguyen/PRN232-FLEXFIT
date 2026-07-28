using System;

namespace FlexFit.Caching;

public static class RedisKeys
{
    public static string RefreshToken(string tokenId)
        => $"flexfit:identity:refresh-token:{tokenId}";

    public static string RefreshTokenFamily(string familyId)
        => $"flexfit:identity:refresh-token-family:{familyId}";

    public static string User(Guid userId)
        => $"flexfit:identity:user:{userId}";

    public static string UserRoles(Guid userId)
        => $"flexfit:identity:user-roles:{userId}";

    public static string EmailOtp(string normalizedEmail, string purpose)
        => $"flexfit:identity:otp:{purpose}:{normalizedEmail}";

    public static string EmailOtpCooldown(string normalizedEmail, string purpose)
        => $"flexfit:identity:otp-cooldown:{purpose}:{normalizedEmail}";

    public static string TokenBlacklist(string jwtId)
        => $"flexfit:identity:token-blacklist:{jwtId}";

    public static string LoginAttempts(string normalizedEmail)
        => $"flexfit:identity:login-attempts:{normalizedEmail}";

    public static string Idempotency(string consumerGroup, Guid eventId)
        => $"flexfit:idempotency:{consumerGroup}:{eventId}";

    public static string DistributedLock(string resource)
        => $"flexfit:lock:{resource}";

    public static string RateLimit(string keyPrefix, string ipOrEmail)
        => $"flexfit:rate-limit:{keyPrefix}:{ipOrEmail}";

    public static string CatalogSession(Guid sessionId)
        => $"flexfit:catalog:sessions:{sessionId}";

    public static string CatalogClass(Guid classId)
        => $"flexfit:catalog:classes:{classId}";

    public static string UserGymBookings(Guid userId)
        => $"flexfit:booking:user:{userId}:gym";

    public static string UserClassBookings(Guid userId)
        => $"flexfit:booking:user:{userId}:class";

    public static string NormalizeEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new ArgumentException("Email to normalize cannot be null or empty.", nameof(email));
        }
        return email.Trim().ToLowerInvariant();
    }
}
