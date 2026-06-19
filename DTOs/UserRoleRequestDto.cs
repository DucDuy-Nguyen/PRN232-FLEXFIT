public class UserRoleRequestDto
{
    public Guid UserId { get; set; }
    public string? Role { get; set; }
    public string? RoleName { get; set; } // Backward-compatible with old clients.
    public Guid? GymId { get; set; }
    public Guid? BranchId { get; set; }
}

