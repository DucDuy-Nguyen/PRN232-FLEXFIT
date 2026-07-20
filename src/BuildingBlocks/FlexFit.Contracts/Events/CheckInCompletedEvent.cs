namespace FlexFit.Contracts.Events;

public sealed class CheckInCompletedEvent
{
    public Guid UserId { get; set; }
    public Guid BookingId { get; set; }
    public string BookingType { get; set; } = null!; // "Class" or "Gym"
    public Guid BranchId { get; set; }
    public string? BranchName { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
