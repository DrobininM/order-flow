using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.DomainEvents;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Handlers;

public sealed class OrderPaidDomainEventHandler : INotificationHandler<OrderPaidDomainEvent>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<OrderPaidDomainEventHandler> _logger;

    public OrderPaidDomainEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        ILogger<OrderPaidDomainEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(OrderPaidDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order {OrderId} paid. Sending notification.", notification.OrderId);

        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException($"User with id {notification.UserId} does not exist");
        }

        await _emailService.SendAsync(
            user.Email.Value,
            "Payment succeeded",
            $"Your order {notification.OrderId} has been paid successfully.",
            cancellationToken);
    }
}
