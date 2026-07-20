using System.ComponentModel.DataAnnotations;

namespace FlexFit.Engagement.Application.DTOs.Promotions;

public class CreatePromotionRequest
{
    [Required(ErrorMessage = "Tiêu đề không được để trống")]
    [StringLength(150)]
    public string Title { get; set; } = null!;
    public string? Description { get; set; }

    [Range(1, 100, ErrorMessage = "Phần trăm giảm giá phải từ 1 đến 100")]
    public int? DiscountPercent { get; set; }

    [Required]
    public DateTime StartDate { get; set; }

    [Required]
    public DateTime EndDate { get; set; }
}
