namespace FlexFit.Identity.Domain.Entities;

/// <summary>
/// UserRole join entity — composite primary key (UserId, RoleId).
///
/// FK constraints (ClientSetNull — monolith uses no cascade delete on roles):
///   FK_UserRoles_Roles:  RoleId → Roles.RoleId
///   FK_UserRoles_Users:  UserId → Users.UserId
///
/// This entity is managed by Identity Service only.
/// Catalog Service references UserId as a plain Guid without FK.
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
