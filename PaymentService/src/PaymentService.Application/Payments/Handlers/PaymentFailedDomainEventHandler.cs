using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Domain.DomainEvents;

namespace PaymentService.Application.Payments.Handlers;

/// <summary>
/// In-process reaction to a failed payment. Extended with notifications or analytics
/// as the service grows; no manual registration is required (MediatR scans the assembly).
/// </summary>
public sealed class PaymentFailedDomainEventHandler : INotificationHandler<PaymentFailedDomainEvent>
{
    private readonly ILogger<PaymentFailedDomainEventHandler> _logger;

    public PaymentFailedDomainEventHandler(ILogger<PaymentFailedDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PaymentFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogWarning(
            "Payment {PaymentId} for order {OrderId} failed.",
            notification.PaymentId, notification.OrderId);

        return Task.CompletedTask;
    }
}
