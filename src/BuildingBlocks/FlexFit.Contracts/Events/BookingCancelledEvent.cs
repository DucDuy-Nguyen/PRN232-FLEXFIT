namespace FlexFit.Contracts.Events;

public sealed class BookingCancelledEvent
{
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }
    public string BookingType { get; set; } = null!; // "Class" or "Gym"
    public string? Reason { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
