namespace PaymentService.Application.IntegrationEvents;

/// <summary>
/// Marker contract for all integration events exchanged over the message bus.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// When the event occurred (UTC).
    /// </summary>
    DateTime OccurredOn { get; }
}

/// <summary>
/// Consumed when an order is ready to be paid.
/// </summary>
public sealed record PaymentRequestedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<PaymentRequestItemDto> Items,
    DateTime OccurredOn) : IIntegrationEvent;

/// <summary>
/// Item snapshot carried by <see cref="PaymentRequestedIntegrationEvent"/>.
/// </summary>
public sealed record PaymentRequestItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

/// <summary>
/// Published when a payment succeeds.
/// </summary>
public sealed record PaymentSucceededIntegrationEvent(
    Guid OrderId,
    string ExternalPaymentId,
    DateTime OccurredOn) : IIntegrationEvent;

/// <summary>
/// Published when a payment fails.
/// </summary>
public sealed record PaymentFailedIntegrationEvent(
    Guid OrderId,
    string Error,
    DateTime OccurredOn) : IIntegrationEvent;
