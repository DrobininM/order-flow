using MediatR;
using Microsoft.Extensions.Logging;
using PaymentService.Domain.DomainEvents;

namespace PaymentService.Application.Payments.Handlers;

/// <summary>
/// In-process reaction to a successful payment. Extended with notifications or analytics
/// as the service grows; no manual registration is required (MediatR scans the assembly).
/// </summary>
public sealed class PaymentSucceededDomainEventHandler : INotificationHandler<PaymentSucceededDomainEvent>
{
    private readonly ILogger<PaymentSucceededDomainEventHandler> _logger;

    public PaymentSucceededDomainEventHandler(ILogger<PaymentSucceededDomainEventHandler> logger)
    {
        _logger = logger;
    }

    public Task Handle(PaymentSucceededDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Payment {PaymentId} for order {OrderId} succeeded.",
            notification.PaymentId, notification.OrderId);

        return Task.CompletedTask;
    }
}
