namespace Flexfit.DTOs
{
    public class UserDto
    {
        public Guid UserId { get; set; }
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }

        public DateTime? DateOfBirth { get; set; }

        public bool IsEmailVerified { get; set; }
        public bool IsActive { get; set; }
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UpdateUserRequest
    {
        public string? FullName { get; set; }
        public string? PhoneNumber { get; set; }
        public string? AvatarUrl { get; set; }

        // THÊM: Ngày sinh cho phép update
        public DateTime? DateOfBirth { get; set; }

        // Tuyệt đối không để IsActive ở đây nữa
    }
}