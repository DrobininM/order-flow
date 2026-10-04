using MediatR;

namespace PaymentService.Domain.Common;

/// <summary>
/// Marker contract for in-process domain events raised by aggregates.
/// </summary>
public interface IDomainEvent : INotification
{
    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    DateTime OccurredOn { get; }
}
