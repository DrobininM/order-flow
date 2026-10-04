using System.Text.Json;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.IntegrationEvents;
using OrderService.Application.Orders.Commands;
using OrderService.Application.Orders.DTOs;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Handlers;

public sealed class CancelOrderCommandHandler : IRequestHandler<CancelOrderCommand, ErrorOr<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly ILogger<CancelOrderCommandHandler> _logger;

    public CancelOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IOutboxMessageRepository outboxRepository,
        ILogger<CancelOrderCommandHandler> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _logger = logger;
    }

    public async Task<ErrorOr<OrderDto>> Handle(CancelOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, cancellationToken);

        if (order is null)
            return Error.NotFound(ErrorCodes.OrderNotFound, $"Order {request.OrderId} not found.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        var reason = request.Reason ?? "Cancelled by user.";

        try
        {
            order.Cancel(reason);
        }
        catch (DomainException ex)
        {
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            
            return Error.Validation(ex.Code, ex.Message);
        }
        
        try
        {
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToArray();
            var products = await _productRepository.GetByIdsWithLockAsync(productIds, cancellationToken);
            var productMap = products.ToDictionary(p => p.Id);

            foreach (var item in order.Items)
            {
                if (!productMap.TryGetValue(item.ProductId, out var product))
                {
                    _logger.LogError("Product {ProductId} not found while cancelling order {OrderId}", item.ProductId, order.Id);

                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);

                    return Error.NotFound(ErrorCodes.ProductNotFound,
                        $"Product {item.ProductId} not found during cancellation.");
                }

                if (product.ReservedQuantity < item.Quantity)
                {
                    _logger.LogWarning(
                        "Cannot release reservation for product {ProductId} in order {OrderId}: insufficient reserved quantity.",
                        item.ProductId, order.Id);

                    throw new InvalidOperationException(
                        $"Cannot release reservation for product {item.ProductId}: product state is inconsistent.");
                }

                product.ReleaseReservation(item.Quantity);
            }

            var integrationEvent = new OrderCancelledIntegrationEvent(
                order.Id, order.UserId, reason, DateTime.UtcNow);

            var outboxMessage = OutboxMessage.Create(
                nameof(OrderCancelledIntegrationEvent),
                JsonSerializer.Serialize(integrationEvent));

            await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            return order.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cancel order {OrderId}.", order.Id);

            await _unitOfWork.RollbackTransactionAsync(cancellationToken);

            throw;
        }
    }
}
