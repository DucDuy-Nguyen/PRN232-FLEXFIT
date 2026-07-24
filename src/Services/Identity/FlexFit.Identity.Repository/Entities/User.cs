using System;
using System.Collections.Generic;


namespace FlexFit.Identity.Repository.Entities;

/// <summary>
/// User aggregate root — the core entity of the Identity domain.
/// </summary>
public sealed class User : AggregateRoot
{
    // Primary Key — Guid, default newid() in SQL Server
    public Guid UserId { get; private set; }

    // Core identity fields — max lengths match SQL schema
    public string FullName { get; private set; } = null!;   // nvarchar(100)
    public string Email { get; private set; } = null!;      // nvarchar(100), UNIQUE
    public string PasswordHash { get; private set; } = null!;
    public string? PhoneNumber { get; private set; }         // nvarchar(20)
    public DateOnly? DateOfBirth { get; private set; }
    public string? AvatarUrl { get; private set; }

    // Email verification state
    public bool IsEmailVerified { get; private set; }

    // Deprecated: Kept temporarily to allow seamless legacy database migration.
    [Obsolete("Use Redis-based OTP instead. Kept for legacy schema/data migration only.")]
    public string? EmailVerificationToken { get; private set; }

    [Obsolete("Use Redis-based OTP instead. Kept for legacy schema/data migration only.")]
    public DateTime? VerificationTokenExpires { get; private set; }

    // Google OAuth linkage — nullable; set on first Google login
    public string? GoogleSubject { get; private set; }   // nvarchar(255), unique filtered index

    // Account state
    public bool IsActive { get; private set; }
    public DateTime? LastLoginAt { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime? UpdatedAt { get; private set; }

    // Navigation — Identity domain only
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();
    public MemberProfile? MemberProfile { get; private set; }

    // EF Core parameterless constructor
    private User() { }

    /// <summary>
    /// Factory method for creating a new user during registration.
    /// PasswordHash must already be hashed before passing in.
    /// </summary>
    public static User Create(
        string fullName,
        string email,
        string passwordHash,
        string? phoneNumber)
    {
        return new User
        {
            UserId = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = passwordHash,
            PhoneNumber = phoneNumber,
            IsEmailVerified = false,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    /// <summary>
    /// Factory method for creating a user via Google OAuth.
    /// Email is pre-verified. No password needed.
    /// </summary>
    public static User CreateFromGoogle(string fullName, string email, string googleSubject)
    {
        return new User
        {
            UserId = Guid.NewGuid(),
            FullName = fullName,
            Email = email,
            PasswordHash = string.Empty,  // Google login has no password
            GoogleSubject = googleSubject,
            IsEmailVerified = true,
            IsActive = true,
            CreatedAt = DateTime.UtcNow
        };
    }

    public void MarkEmailVerified()
    {
        IsEmailVerified = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void AddUserRole(UserRole userRole)
    {
        _userRoles.Add(userRole);
    }

    public void UpdateProfile(string? fullName, string? phoneNumber, DateOnly? dateOfBirth, string? avatarUrl)
    {
        FullName = fullName ?? FullName;
        PhoneNumber = phoneNumber ?? PhoneNumber;
        DateOfBirth = dateOfBirth ?? DateOfBirth;
        AvatarUrl = avatarUrl ?? AvatarUrl;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetPasswordHash(string newPasswordHash)
    {
        PasswordHash = newPasswordHash;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetActive(bool isActive)
    {
        IsActive = isActive;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Activate()
    {
        IsActive = true;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Deactivate()
    {
        IsActive = false;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Links a Google subject (sub claim) to this user account.
    /// Only links if not already linked. Never overwrites an existing subject.
    /// </summary>
    public void SetGoogleSubject(string googleSubject)
    {
        if (string.IsNullOrWhiteSpace(googleSubject)) return;
        if (GoogleSubject != null) return; // already linked — do not overwrite
        GoogleSubject = googleSubject;
        UpdatedAt = DateTime.UtcNow;
    }

    public void RecordLogin()
    {
        LastLoginAt = DateTime.UtcNow;
    }
}
