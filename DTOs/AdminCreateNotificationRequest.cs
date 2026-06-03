using System;
using System.ComponentModel.DataAnnotations;

namespace Flexfit.DTOs.Notification
{
    public class AdminCreateNotificationRequest
    {
        // Nếu UserId có giá trị -> Gửi cho 1 người. Nếu null -> Gửi cho toàn bộ hệ thống
        public Guid? UserId { get; set; }

        [Required(ErrorMessage = "Tiêu đề thông báo không được để trống.")]
        [StringLength(200, ErrorMessage = "Tiêu đề không được vượt quá 200 ký tự.")]
        public string Title { get; set; } = null!;

        [Required(ErrorMessage = "Nội dung thông báo không được để trống.")]
        public string Content { get; set; } = null!;

        [Required(ErrorMessage = "Loại thông báo không được để trống.")]
        public string Type { get; set; } = null!; // Ví dụ: "AccountUpdate", "SystemAlert", v.v.
    }
}