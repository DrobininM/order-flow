namespace PaymentService.Domain.Common;

/// <summary>
/// Base class for aggregate roots. Collects domain events raised by the aggregate so that
/// the unit of work can dispatch them after the business change is persisted.
/// </summary>
public abstract class AggregateRoot<TId> : Entity<TId>, IAggregateRoot
    where TId : notnull
{
    private readonly List<IDomainEvent> _domainEvents = [];

    protected AggregateRoot() { }

    protected AggregateRoot(TId id) : base(id) { }

    /// <summary>
    /// Registers a domain event to be dispatched when the unit of work saves changes.
    /// </summary>
    protected void AddDomainEvent(IDomainEvent domainEvent) =>
        _domainEvents.Add(domainEvent);

    /// <summary>
    /// Returns the pending domain events and clears the internal buffer.
    /// Called by the unit of work so each event is dispatched at most once per save.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DequeueDomainEvents()
    {
        var events = _domainEvents.ToList();
        _domainEvents.Clear();

        return events;
    }
}
