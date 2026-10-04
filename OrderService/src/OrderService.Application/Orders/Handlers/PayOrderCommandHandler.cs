using System.Text.Json;
using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.IntegrationEvents;
using OrderService.Application.Orders.Commands;
using OrderService.Application.Orders.DTOs;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Handlers;

public sealed class PayOrderCommandHandler : IRequestHandler<PayOrderCommand, ErrorOr<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly ILogger<PayOrderCommandHandler> _logger;
    private readonly IOptions<OrderOptions> _orderOptions;

    public PayOrderCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        IOutboxMessageRepository outboxRepository,
        ILogger<PayOrderCommandHandler> logger,
        IOptions<OrderOptions> orderOptions)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _outboxRepository = outboxRepository;
        _logger = logger;
        _orderOptions = orderOptions;
    }

    public async Task<ErrorOr<OrderDto>> Handle(PayOrderCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, cancellationToken);

        if (order is null)
            return Error.NotFound(ErrorCodes.OrderNotFound, $"Order {request.OrderId} not found.");

        if (order.UserId != request.UserId)
            return Error.Forbidden(ErrorCodes.OrderNotOwned, "Orders can be payed only by their owners.");

        await _unitOfWork.BeginTransactionAsync(cancellationToken);

        try
        {
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _productRepository.GetByIdsWithLockAsync(productIds, cancellationToken);
            var productMap = products.ToDictionary(p => p.Id);

            foreach (var item in order.Items)
            {
                if (!productMap.TryGetValue(item.ProductId, out var product))
                {
                    _logger.LogError("Product {ProductId} not found during payment of order {OrderId}", item.ProductId, order.Id);
                    
                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    
                    return Error.NotFound(ErrorCodes.ProductNotFound, $"Product {item.ProductId} not found.");
                }

                if (!product.CanReserve(item.Quantity))
                {
                    _logger.LogWarning("Insufficient stock for product {ProductId} in order {OrderId}. Available: {Available}, Requested: {Requested}",
                        item.ProductId, order.Id, product.AvailableQuantity, item.Quantity);

                    await _unitOfWork.RollbackTransactionAsync(cancellationToken);
                    
                    return Error.Validation(ErrorCodes.ProductInsufficientStock,
                        $"Insufficient stock for product {item.ProductId}. Available: {product.AvailableQuantity}.");
                }

                product.Reserve(item.Quantity);
            }

            var reservationTimeout = TimeSpan.FromMinutes(_orderOptions.Value.ReservationTimeoutMinutes);
            
            order.MarkAsReserved(DateTime.UtcNow.Add(reservationTimeout));

            var total = order.GetTotalAmount();

            var integrationEvent = new PaymentRequestedIntegrationEvent(
                order.Id, order.UserId, total.Amount, total.Currency.ToString(),
                order.Items.Select(i => i.ToIntegrationDto()).ToList(),
                DateTime.UtcNow);

            var outboxMessage = OutboxMessage.Create(
                nameof(PaymentRequestedIntegrationEvent),
                JsonSerializer.Serialize(integrationEvent));

            await _outboxRepository.AddAsync(outboxMessage, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            
            await _unitOfWork.CommitTransactionAsync(cancellationToken);

            _logger.LogInformation("Payment requested for order {OrderId}", order.Id);

            return order.ToDto();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Payment processing failed for order {OrderId}", order.Id);
            
            await _unitOfWork.RollbackTransactionAsync(cancellationToken);
            
            throw;
        }
    }
}
