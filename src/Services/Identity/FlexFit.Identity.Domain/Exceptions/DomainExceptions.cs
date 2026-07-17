namespace FlexFit.Identity.Domain.Exceptions;

/// <summary>
/// Base exception for all domain-level business rule violations in the Identity domain.
/// These exceptions represent invalid domain state, not infrastructure failures.
/// </summary>
public abstract class DomainException : Exception
{
    protected DomainException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a User entity is not found by the given criteria.
/// </summary>
public sealed class UserNotFoundException : DomainException
{
    public UserNotFoundException(Guid userId)
        : base($"User with ID '{userId}' was not found.") { }

    public UserNotFoundException(string email)
        : base($"User with email '{email}' was not found.") { }
}

/// <summary>
/// Thrown when attempting to register an email that already exists.
/// </summary>
public sealed class EmailAlreadyExistsException : DomainException
{
    public EmailAlreadyExistsException(string email)
        : base($"Email '{email}' is already registered.") { }
}

/// <summary>
/// Thrown when email OTP validation fails (wrong code, expired, or max attempts reached).
/// </summary>
public sealed class OtpValidationException : DomainException
{
    public OtpValidationException(string message) : base(message) { }
}

/// <summary>
/// Thrown when a refresh token is invalid, expired, or has been revoked.
/// </summary>
public sealed class InvalidRefreshTokenException : DomainException
{
    public InvalidRefreshTokenException()
        : base("Refresh token is invalid, expired, or has been revoked.") { }
}

/// <summary>
/// Thrown when a refresh token reuse attack is detected.
/// This triggers revocation of the entire token family.
/// </summary>
public sealed class RefreshTokenReuseException : DomainException
{
    public RefreshTokenReuseException(string familyId)
        : base($"Refresh token reuse detected for family '{familyId}'. All sessions have been revoked.") { }
}

/// <summary>
/// Thrown when account is not active (locked or pending verification).
/// </summary>
public sealed class AccountNotActiveException : DomainException
{
    public AccountNotActiveException()
        : base("Account is inactive or has been locked.") { }
}
