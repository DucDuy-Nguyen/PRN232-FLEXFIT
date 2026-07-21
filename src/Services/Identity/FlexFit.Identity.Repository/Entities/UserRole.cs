using System;

namespace FlexFit.Identity.Repository.Entities;

/// <summary>
/// UserRole join entity - composite primary key (UserId, RoleId).
/// </summary>
public sealed class UserRole
{
    public Guid UserId { get; private set; }
    public Guid RoleId { get; private set; }
    public DateTime AssignedAt { get; private set; }

    // Navigation
    public User User { get; private set; } = null!;
    public Role Role { get; private set; } = null!;

    // EF Core constructor
    private UserRole() { }

    public static UserRole Create(Guid userId, Guid roleId)
    {
        return new UserRole
        {
            UserId = userId,
            RoleId = roleId,
            AssignedAt = DateTime.UtcNow
        };
    }
}
