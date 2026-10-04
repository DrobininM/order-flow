namespace PaymentService.Domain.Common;

/// <summary>
/// Identifies an aggregate root, allowing the unit of work to collect its pending domain events.
/// </summary>
public interface IAggregateRoot
{
    /// <summary>
    /// Extract all domain events of entity.
    /// </summary>
    IReadOnlyList<IDomainEvent> DequeueDomainEvents();
}
