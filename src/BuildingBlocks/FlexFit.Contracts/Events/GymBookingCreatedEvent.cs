namespace FlexFit.Contracts.Events;

public sealed class GymBookingCreatedEvent
{
    public Guid BookingId { get; set; }
    public Guid UserId { get; set; }
    public Guid BranchId { get; set; }
    public string? GymName { get; set; }
    public string? BranchName { get; set; }
    public DateTime BookingDate { get; set; }
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
