using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using FlexFit.Identity.Application.Abstractions;

namespace FlexFit.Identity.Infrastructure.Security;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;       // 128-bit salt
    private const int HashSize = 32;       // 256-bit hash
    private const int Iterations = 10000;  // Match monolith configuration

    public string Hash(string password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));

        // Generate 16-byte cryptographically secure random salt
        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        // Derive key using PBKDF2 + HMACSHA256
        var hash = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            Iterations,
            HashSize);

        // Format: base64(salt).base64(hash)
        return Convert.ToBase64String(salt) + "." + Convert.ToBase64String(hash);
    }

    public bool Verify(string password, string storedHash)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));
        if (string.IsNullOrWhiteSpace(storedHash)) return false;

        try
        {
            var parts = storedHash.Split('.');
            if (parts.Length != 2)
            {
                return false;
            }

            var salt = Convert.FromBase64String(parts[0]);
            var hash = Convert.FromBase64String(parts[1]);

            // Recompute PBKDF2 hash using the same parameters and salt
            var computedHash = KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA256,
                Iterations,
                HashSize);

            // Constant-time comparison to mitigate timing attacks
            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }
        catch (Exception)
        {
            // Fail gracefully if storedHash format is corrupted/invalid Base64
            return false;
        }
    }
}
