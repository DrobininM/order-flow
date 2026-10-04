using PaymentService.Domain.Common;

namespace PaymentService.Domain.DomainEvents;

/// <summary>
/// Raised when a new payment is registered for an order.
/// </summary>
public sealed record PaymentCreatedDomainEvent(
    Guid PaymentId,
    Guid OrderId,
    decimal Amount,
    string Currency,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when a payment is successfully processed by the external gateway.
/// </summary>
public sealed record PaymentSucceededDomainEvent(
    Guid PaymentId,
    Guid OrderId,
    string ExternalPaymentId,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when a payment is rejected by the external gateway.
/// </summary>
public sealed record PaymentFailedDomainEvent(
    Guid PaymentId,
    Guid OrderId,
    string Error,
    DateTime OccurredOn) : IDomainEvent;

/// <summary>
/// Raised when a successful payment is refunded.
/// </summary>
public sealed record PaymentRefundedDomainEvent(
    Guid PaymentId,
    Guid OrderId,
    DateTime OccurredOn) : IDomainEvent;
