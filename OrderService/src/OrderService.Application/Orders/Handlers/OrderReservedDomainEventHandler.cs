using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.DomainEvents;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Handlers;

public sealed class OrderReservedDomainEventHandler : INotificationHandler<OrderReservedDomainEvent>
{
    private readonly IEmailService _emailService;
    private readonly IUserRepository _userRepository;
    private readonly ILogger<OrderReservedDomainEventHandler> _logger;

    public OrderReservedDomainEventHandler(
        IEmailService emailService,
        IUserRepository userRepository,
        ILogger<OrderReservedDomainEventHandler> logger)
    {
        _emailService = emailService;
        _userRepository = userRepository;
        _logger = logger;
    }

    public async Task Handle(OrderReservedDomainEvent notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("Order {OrderId} reserved for user {UserId}. Sending notification.",
            notification.OrderId, notification.UserId);

        var user = await _userRepository.GetByIdAsync(notification.UserId, cancellationToken);

        if (user is null)
        {
            throw new InvalidOperationException($"User with id {notification.UserId} does not exist");
        }
        
        var email = user.Email.Value;

        await _emailService.SendAsync(
            email,
            "Payment processing started",
            $"Your order {notification.OrderId} payment is being processed.",
            cancellationToken);
    }
}
