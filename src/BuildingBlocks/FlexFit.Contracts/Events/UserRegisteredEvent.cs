namespace FlexFit.Contracts.Events;

public sealed class UserRegisteredEvent
{
    public Guid UserId { get; set; }
    public string FullName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public DateTime OccurredAt { get; set; } = DateTime.UtcNow;
}
