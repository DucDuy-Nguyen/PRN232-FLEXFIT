using System;
using System.Collections.Generic;

namespace FlexFit.Identity.API.Models.Entities;

/// <summary>
/// Role entity - lookup table for role names.
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
