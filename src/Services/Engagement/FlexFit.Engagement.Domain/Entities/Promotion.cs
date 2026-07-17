namespace FlexFit.Engagement.Domain.Entities;

public class Promotion
{
    public Guid PromotionId { get; set; }
    public string Title { get; set; } = null!;
    public string? Description { get; set; }
    public int? DiscountPercent { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
