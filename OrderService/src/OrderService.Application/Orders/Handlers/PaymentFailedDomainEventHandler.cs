using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.DomainEvents;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Handlers;

public sealed class PaymentFailedDomainEventHandler : INotificationHandler<PaymentFailedDomainEvent>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<PaymentFailedDomainEventHandler> _logger;

    public PaymentFailedDomainEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        ILogger<PaymentFailedDomainEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(PaymentFailedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation(
            "Payment for order {OrderId} failed: {Error}. Sending notification.",
            notification.OrderId, notification.Error);

        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException($"User with id {notification.UserId} does not exist");
        }

        await _emailService.SendAsync(
            user.Email.Value,
            "Payment failed",
            $"Payment for your order {notification.OrderId} failed: {notification.Error}. You can try again.",
            cancellationToken);
    }
}
