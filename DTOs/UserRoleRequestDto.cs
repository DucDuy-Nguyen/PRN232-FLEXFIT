public class UserRoleRequestDto
{
    public Guid UserId { get; set; }
    public string RoleName { get; set; } = null!; // Ví dụ: "Admin", "Staff"...
}