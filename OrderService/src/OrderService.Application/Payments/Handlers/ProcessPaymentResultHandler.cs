using System.Text.Json;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.IntegrationEvents;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Payments.Handlers;

/// <summary>
/// Applies the outcome of an external payment to the corresponding order.
/// Consumes <c>payment.succeeded</c> / <c>payment.failed</c> integration events idempotently.
/// </summary>
public sealed class ProcessPaymentResultHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IInboxMessageRepository _inboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessPaymentResultHandler> _logger;

    public ProcessPaymentResultHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IInboxMessageRepository inboxRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessPaymentResultHandler> logger)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _inboxRepository = inboxRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task HandleAsync(string messageId, bool paymentSucceeded, string payload, CancellationToken ct)
    {
        if (await _inboxRepository.ExistsAsync(messageId, ct))
        {
            _logger.LogInformation("Payment result message {MessageId} already processed. Skipping.", messageId);
            
            return;
        }

        var inboxMessage = InboxMessage.Create(
            messageId,
            paymentSucceeded ? nameof(PaymentSucceededIntegrationEvent) : nameof(PaymentFailedIntegrationEvent),
            payload);

        await _inboxRepository.AddAsync(inboxMessage, ct);

        var orderId = ResolveOrderId(payload, paymentSucceeded);

        if (orderId is null)
        {
            _logger.LogError("Unable to deserialize payment result payload: {Payload}", payload);
            inboxMessage.MarkAsProcessed();
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        var order = await _orderRepository.GetByIdWithItemsAsync(orderId.Value, ct);

        if (order is null)
        {
            _logger.LogWarning("Order {OrderId} not found for payment result.", orderId);
            inboxMessage.MarkAsProcessed();
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        if (order.Status != OrderStatus.Reserved)
        {
            _logger.LogInformation(
                "Order {OrderId} is in status {Status}; payment result ignored as already handled.",
                order.Id, order.Status);

            inboxMessage.MarkAsProcessed();
            await _unitOfWork.SaveChangesAsync(ct);
            return;
        }

        await _unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();
            var products = await _productRepository.GetByIdsWithLockAsync(productIds, ct);
            var productMap = products.ToDictionary(p => p.Id);

            foreach (var item in order.Items)
            {
                if (!productMap.TryGetValue(item.ProductId, out var product))
                {
                    _logger.LogError(
                        "Product {ProductId} not found while applying payment result to order {OrderId}.",
                        item.ProductId, order.Id);

                    throw new InvalidOperationException(
                        $"Product {item.ProductId} not found while applying payment result to order {order.Id}.");
                }

                if (paymentSucceeded)
                    product.CommitReservation(item.Quantity);
                else
                    product.ReleaseReservation(item.Quantity);
            }

            if (paymentSucceeded)
                order.MarkAsPaid();
            else
                order.MarkAsPaymentFailed("Payment failed.");

            inboxMessage.MarkAsProcessed();

            await _unitOfWork.SaveChangesAsync(ct);
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Payment result ({Result}) applied to order {OrderId}.",
                paymentSucceeded ? "succeeded" : "failed", order.Id);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to apply payment result to order {OrderId}.", order.Id);
            await _unitOfWork.RollbackTransactionAsync(ct);
            throw;
        }
    }

    private static Guid? ResolveOrderId(string payload, bool paymentSucceeded)
    {
        if (paymentSucceeded)
        {
            var message = JsonSerializer.Deserialize<PaymentSucceededIntegrationEvent>(payload, JsonOptions);
            return message?.OrderId;
        }

        var failed = JsonSerializer.Deserialize<PaymentFailedIntegrationEvent>(payload, JsonOptions);
        return failed?.OrderId;
    }
}
