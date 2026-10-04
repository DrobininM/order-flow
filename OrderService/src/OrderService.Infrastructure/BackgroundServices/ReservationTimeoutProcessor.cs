using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Common;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.BackgroundServices;

public sealed class ReservationTimeoutProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<ReservationTimeoutProcessor> _logger;
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(30);

    public ReservationTimeoutProcessor(IServiceScopeFactory scopeFactory, ILogger<ReservationTimeoutProcessor> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("ReservationTimeoutProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredReservationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing expired reservations.");
            }

            await Task.Delay(ProcessingDelay, stoppingToken);
        }
    }

    private async Task ProcessExpiredReservationsAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var productRepository = scope.ServiceProvider.GetRequiredService<IProductRepository>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<Application.Common.Interfaces.IUnitOfWork>();

        // Hold an explicit transaction so the row locks taken below are kept until the batch is saved.
        await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var threshold = DateTime.UtcNow;

            // Lock the batch so concurrent instances skip these orders (SELECT ... FOR UPDATE SKIP LOCKED).
            var expiredOrders = await orderRepository.GetExpiredReservationsWithLockAsync(threshold, ct);

            foreach (var order in expiredOrders)
            {
                _logger.LogWarning("Order {OrderId} reservation expired. Cancelling.", order.Id);

                var productIds = order.Items.Select(i => i.ProductId).Distinct().ToList();

                // Lock the products (SELECT ... FOR UPDATE) before releasing stock to avoid
                // races with concurrent payments/reservations.
                var products = await productRepository.GetByIdsWithLockAsync(productIds, ct);
                var productMap = products.ToDictionary(p => p.Id);

                foreach (var item in order.Items)
                {
                    if (!productMap.TryGetValue(item.ProductId, out var product))
                    {
                        _logger.LogError("Product {ProductId} not found while releasing reservation for order {OrderId}.",
                            item.ProductId, order.Id);

                        continue;
                    }

                    try
                    {
                        product.ReleaseReservation(item.Quantity);
                    }
                    catch (DomainException ex)
                    {
                        _logger.LogError(ex, "Failed to release {Quantity} reserved units of product {ProductId} for order {OrderId}.",
                            item.Quantity, item.ProductId, order.Id);
                    }
                }

                try
                {
                    order.Cancel("Reservation timeout expired.");
                }
                catch (DomainException ex)
                {
                    _logger.LogError(ex, "Failed to cancel expired order {OrderId}.", order.Id);
                }
            }

            if (expiredOrders.Count > 0)
                await unitOfWork.SaveChangesAsync(ct);

            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);

            throw;
        }
    }
}
