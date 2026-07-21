using System.ComponentModel.DataAnnotations;

namespace FlexFit.Engagement.API.DTOs.Notifications;

public class AdminCreateNotificationRequest
{
    public Guid? UserId { get; set; } // Null if sending broadcast to all users

    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(150, ErrorMessage = "Tiêu đề không được quá 150 ký tự")]
    public string Title { get; set; } = null!;

    [Required(ErrorMessage = "Nội dung không được để trống")]
    public string Content { get; set; } = null!;

    [StringLength(50)]
    public string? Type { get; set; } = "SystemAlert";
}
