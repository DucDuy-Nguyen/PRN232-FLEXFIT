namespace FlexFit.Catalog.Service.DTOs;

public class BookingSnapshotDto
{
    public string ResourceId { get; set; } = null!;
    public string ResourceType { get; set; } = null!;
    public string GymId { get; set; } = null!;
    public string GymName { get; set; } = null!;
    public string BranchId { get; set; } = null!;
    public string BranchName { get; set; } = null!;
    public string Title { get; set; } = null!;
    public string StartTime { get; set; } = null!;
    public string EndTime { get; set; } = null!;
    public int CreditCost { get; set; }
    public int Capacity { get; set; }
    public string Status { get; set; } = null!;
    public bool IsActive { get; set; }
}
