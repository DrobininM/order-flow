namespace OrderService.Application.IntegrationEvents;

/// <summary>
/// Marker contract for all integration events published to the message bus.
/// </summary>
public interface IIntegrationEvent
{
    /// <summary>
    /// When the event occurred.
    /// </summary>
    DateTime OccurredOn { get; }
}

/// <summary>
/// Published when a new order is created.
/// </summary>
public sealed record OrderCreatedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<OrderItemDto> Items,
    DateTime OccurredOn) : IIntegrationEvent;

public sealed record OrderItemDto(Guid ProductId, string ProductName, int Quantity, decimal UnitPrice);

/// <summary>
/// Published when an order is canceled.
/// </summary>
public sealed record OrderCancelledIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    string Reason,
    DateTime OccurredOn) : IIntegrationEvent;

/// <summary>
/// Published when a payment is requested (before external processing).
/// </summary>
public sealed record PaymentRequestedIntegrationEvent(
    Guid OrderId,
    Guid UserId,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<OrderItemDto> Items,
    DateTime OccurredOn) : IIntegrationEvent;

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
