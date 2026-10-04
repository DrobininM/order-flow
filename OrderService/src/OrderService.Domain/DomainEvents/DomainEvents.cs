using OrderService.Domain.Common;

namespace OrderService.Domain.DomainEvents;

public sealed record OrderCreatedDomainEvent(Guid OrderId, Guid UserId, DateTime OccurredOn) : IDomainEvent;

public sealed record OrderReservedDomainEvent(Guid OrderId, Guid UserId, DateTime OccurredOn) : IDomainEvent;

public sealed record OrderPaidDomainEvent(Guid OrderId, Guid UserId, DateTime OccurredOn) : IDomainEvent;

public sealed record OrderCancelledDomainEvent(Guid OrderId, Guid UserId, string Reason, DateTime OccurredOn) : IDomainEvent;

public sealed record PaymentSucceededDomainEvent(Guid OrderId, string ExternalPaymentId, DateTime OccurredOn) : IDomainEvent;

public sealed record PaymentFailedDomainEvent(Guid OrderId, Guid UserId, string Error, DateTime OccurredOn) : IDomainEvent;

public sealed record ProductReservedDomainEvent(Guid ProductId, int Quantity, DateTime OccurredOn) : IDomainEvent;
