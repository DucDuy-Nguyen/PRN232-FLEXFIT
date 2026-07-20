namespace FlexFit.Engagement.API.Models.DTOs.Promotions;

public class PromotionResponse
{
    public Guid PromotionId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int? DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public string Status { get; set; } = null!; // "Active", "Expired", "NotStarted"
}
