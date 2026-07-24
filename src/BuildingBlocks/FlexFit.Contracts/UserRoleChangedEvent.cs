using System;

namespace FlexFit.Contracts;

public sealed record UserRoleChangedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string RoleName { get; init; } = null!;
    public string Action { get; init; } = null!; // "Assigned" or "Revoked"
    public Guid ChangedBy { get; init; }
}
