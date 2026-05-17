using System;
using System.Collections.Generic;

namespace Flexfit.Models;

public partial class User
{
    public Guid UserId { get; set; }

    public string FullName { get; set; } = null!;

    public string Email { get; set; } = null!;

    public string PasswordHash { get; set; } = null!;

    public string? PhoneNumber { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsEmailVerified { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }
    public DateTime? DateOfBirth { get; set; }

    public virtual ICollection<Gym> Gyms { get; set; } = new List<Gym>();

    public virtual MemberProfile? MemberProfile { get; set; }
    public string? EmailVerificationToken { get; set; }
    public DateTime? VerificationTokenExpires { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();
    public virtual ICollection<BranchStaff> BranchStaffs { get; set; } = new List<BranchStaff>();
}
