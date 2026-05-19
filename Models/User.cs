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

    public DateOnly? DateOfBirth { get; set; }

    public string? AvatarUrl { get; set; }

    public bool IsEmailVerified { get; set; }

    public string? EmailVerificationToken { get; set; }

    public DateTime? VerificationTokenExpires { get; set; }

    public bool IsActive { get; set; }

    public DateTime? LastLoginAt { get; set; }

    public DateTime CreatedAt { get; set; }

    public DateTime? UpdatedAt { get; set; }

    public virtual ICollection<BranchStaff> BranchStaffs { get; set; } = new List<BranchStaff>();


    public virtual ICollection<CheckInLog> CheckInLogScannedByNavigations { get; set; } = new List<CheckInLog>();

    public virtual ICollection<CheckInLog> CheckInLogUsers { get; set; } = new List<CheckInLog>();

    public virtual ICollection<ClassBooking> ClassBookingCheckedInByNavigations { get; set; } = new List<ClassBooking>();

    public virtual ICollection<ClassBooking> ClassBookingUsers { get; set; } = new List<ClassBooking>();

    public virtual ICollection<CreditTransaction> CreditTransactions { get; set; } = new List<CreditTransaction>();

    public virtual ICollection<FavoriteGym> FavoriteGyms { get; set; } = new List<FavoriteGym>();

    public virtual ICollection<GymBooking> GymBookingCheckedInByNavigations { get; set; } = new List<GymBooking>();

    public virtual ICollection<GymBooking> GymBookingUsers { get; set; } = new List<GymBooking>();

    public virtual ICollection<Gym> Gyms { get; set; } = new List<Gym>();

    public virtual MemberProfile? MemberProfile { get; set; }

    public virtual ICollection<Notification> Notifications { get; set; } = new List<Notification>();

    public virtual ICollection<Payment> Payments { get; set; } = new List<Payment>();

    public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

    public virtual ICollection<SystemLog> SystemLogs { get; set; } = new List<SystemLog>();

    public virtual UserCredit? UserCredit { get; set; }

    public virtual ICollection<UserRole> UserRoles { get; set; } = new List<UserRole>();

    public virtual ICollection<UserWorkoutHistory> UserWorkoutHistories { get; set; } = new List<UserWorkoutHistory>();
}
