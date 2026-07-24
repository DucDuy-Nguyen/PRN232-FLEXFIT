using System;
using System.Collections.Generic;

namespace FlexFit.Identity.Repository.Entities;

/// <summary>
/// Base class for aggregate roots in the Identity domain.
/// Supports domain event collection for future event-sourcing patterns.
/// </summary>
public abstract class AggregateRoot
{
    private readonly List<IDomainEvent> _domainEvents = new();

    public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    protected void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }
}
