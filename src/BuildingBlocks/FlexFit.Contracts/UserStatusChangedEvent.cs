using System;

namespace FlexFit.Contracts;

public sealed record UserStatusChangedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = null!;
    public bool OldIsActive { get; init; }
    public bool NewIsActive { get; init; }
    public Guid ChangedBy { get; init; }
}
