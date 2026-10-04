namespace OrderService.Domain.Common;

public interface IAggregateRoot
{
    /// <summary>
    /// Extract all domain events of entity.
    /// </summary>
    IReadOnlyList<IDomainEvent> DequeueDomainEvents();
}
