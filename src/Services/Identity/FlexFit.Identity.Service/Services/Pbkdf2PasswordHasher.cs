using System;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using FlexFit.Identity.Service.Interfaces;

namespace FlexFit.Identity.Service.Services;

public sealed class Pbkdf2PasswordHasher : IPasswordHasher
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 10000;

    public string Hash(string password)
    {
        if (password == null) throw new ArgumentNullException(nameof(password));

        var salt = RandomNumberGenerator.GetBytes(SaltSize);

        var hash = KeyDerivation.Pbkdf2(
            password,
            salt,
            KeyDerivationPrf.HMACSHA256,
            Iterations,
            HashSize);

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

            var computedHash = KeyDerivation.Pbkdf2(
                password,
                salt,
                KeyDerivationPrf.HMACSHA256,
                Iterations,
                HashSize);

            return CryptographicOperations.FixedTimeEquals(hash, computedHash);
        }
        catch (Exception)
        {
            return false;
        }
    }
}
