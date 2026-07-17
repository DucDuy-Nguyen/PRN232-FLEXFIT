namespace FlexFit.Identity.Application.Abstractions;

/// <summary>
/// Password hashing abstraction — defined in Application, implemented in Infrastructure.
///
/// IMPORTANT: The hash format used by Infrastructure must remain backward-compatible
/// with the monolith format: base64(salt16bytes).base64(pbkdf2_hash32bytes)
/// This is required to verify passwords of existing users in the database.
///
/// Algorithm in monolith: PBKDF2 + HMACSHA256 + 16-byte random salt + 10,000 iterations + 32-byte output
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password using PBKDF2.
    /// Returns format: base64(salt).base64(hash)
    /// </summary>
    string Hash(string password);

    /// <summary>
    /// Verifies a plaintext password against a stored hash.
    /// Must be compatible with hashes created by the monolith.
    /// </summary>
    bool Verify(string password, string storedHash);
}
