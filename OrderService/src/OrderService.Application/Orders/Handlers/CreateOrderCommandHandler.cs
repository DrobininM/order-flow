using System.Text.Json;
using ErrorOr;
using MediatR;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.IntegrationEvents;
using OrderService.Application.Orders.Commands;
using OrderService.Application.Orders.DTOs;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Handlers;

public sealed class CreateOrderCommandHandler : IRequestHandler<CreateOrderCommand, ErrorOr<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxMessageRepository _outboxRepository;

    public CreateOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IOutboxMessageRepository outboxRepository)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
    }

    public async Task<ErrorOr<OrderDto>> Handle(CreateOrderCommand request, CancellationToken cancellationToken)
    {
        var productIds = request.Items.Select(i => i.ProductId);
        var products = await _productRepository.GetByIdsAsync(productIds, cancellationToken);

        var productMap = products.ToDictionary(p => p.Id);
        var errors = new List<Error>();

        foreach (var item in request.Items)
        {
            if (!productMap.TryGetValue(item.ProductId, out var product))
            {
                errors.Add(Error.NotFound(ErrorCodes.ProductNotFound, $"Product {item.ProductId} not found."));

                continue;
            }

            if (!product.IsAvailable)
                errors.Add(Error.Validation(ErrorCodes.ProductUnavailable, $"Product '{product.Name}' is unavailable."));

            if (product.AvailableQuantity < item.Quantity)
            {
                errors.Add(Error.Validation(ErrorCodes.ProductInsufficientStock,
                    $"Insufficient stock for '{product.Name}'. Available: {product.AvailableQuantity}, requested: {item.Quantity}."));
            }
        }

        if (errors.Count != 0)
            return errors;

        var order = Order.Create(request.UserId);

        foreach (var item in request.Items)
        {
            var product = productMap[item.ProductId];
            order.AddItem(product.Id, product.Name, product.Price, item.Quantity);
        }

        await _orderRepository.AddAsync(order, cancellationToken);

        var total = order.GetTotalAmount();

        var integrationEvent = new OrderCreatedIntegrationEvent(
            order.Id, order.UserId, total.Amount, total.Currency.ToString(),
            order.Items.Select(i => i.ToIntegrationDto()).ToList(),
            DateTime.UtcNow);

        var outboxMessage = OutboxMessage.Create(
            nameof(OrderCreatedIntegrationEvent),
            JsonSerializer.Serialize(integrationEvent));

        await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
