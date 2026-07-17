namespace FlexFit.Identity.Domain.Entities;

/// <summary>
/// Role entity — lookup table for role names.
///
/// ID type: Guid (matches monolith RoleId)
/// PK: PK__Roles__8AFACE1AD46A3A12
/// Unique index: RoleName — UQ__Roles__8A2B6160ACCD1031, nvarchar(50)
///
/// Known role names in the system (from monolith):
///   - "Admin"
///   - "Member"
///   - "GymPartner"
///   - "Staff"
/// </summary>
public sealed class Role
{
    public Guid RoleId { get; private set; }
    public string RoleName { get; private set; } = null!;   // nvarchar(50), UNIQUE
    public string? Description { get; private set; }        // nvarchar(255)
    public DateTime CreatedAt { get; private set; }

    // Navigation
    private readonly List<UserRole> _userRoles = new();
    public IReadOnlyCollection<UserRole> UserRoles => _userRoles.AsReadOnly();

    // EF Core constructor
    private Role() { }
}
