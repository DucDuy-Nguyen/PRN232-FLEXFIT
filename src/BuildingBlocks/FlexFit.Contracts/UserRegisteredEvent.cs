using System;

namespace FlexFit.Contracts;

public sealed record UserRegisteredEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string FullName { get; init; } = null!;
    public string Email { get; init; } = null!;
    public string? PhoneNumber { get; init; }
    public DateTime CreatedAt { get; init; }
}
