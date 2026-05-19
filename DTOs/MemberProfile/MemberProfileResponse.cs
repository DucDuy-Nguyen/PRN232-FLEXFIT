using System;

namespace Flexfit.DTOs.MemberProfile
{
    public class MemberProfileResponse
    {
        public Guid MemberProfileId { get; set; }
        public Guid UserId { get; set; }

        // Thông tin lấy từ bảng User
        public string FullName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? PhoneNumber { get; set; }
        public DateOnly? DateOfBirth { get; set; }

        // Thông tin từ bảng MemberProfile
        public string? Gender { get; set; }
  
        public decimal? HeightCm { get; set; }
        public decimal? WeightKg { get; set; }
        public string? FitnessGoal { get; set; }
        public string? ActivityLevel { get; set; }
        public string? PreferredWorkoutTime { get; set; }
        public string? Bio { get; set; }
    }

    public class UpdateMemberProfileRequest
    {
        // Cho phép cập nhật cả Họ tên & SĐT ở trang hồ sơ
        public required string FullName { get; set; }
        public string? PhoneNumber { get; set; }

        // Các chỉ số sức khỏe & mục tiêu
        public string? Gender { get; set; }
        public DateOnly? DateOfBirth { get; set; }
        public decimal? HeightCm { get; set; }
        public decimal? WeightKg { get; set; }
        public string? FitnessGoal { get; set; }
        public string? ActivityLevel { get; set; }
        public string? PreferredWorkoutTime { get; set; }
        public string? Bio { get; set; }
    }
}