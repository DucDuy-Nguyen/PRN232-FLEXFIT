using System;

namespace FlexFit.Contracts;

public sealed record EmailVerifiedEvent : IntegrationEvent
{
    public Guid UserId { get; init; }
    public string Email { get; init; } = null!;
}
