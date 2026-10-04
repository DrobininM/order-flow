using OrderService.Domain.Entities;

namespace OrderService.Domain.Repositories;

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Returns an order with all its items eagerly loaded.
    /// </summary>
    Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default);

    Task<List<Order>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken ct = default);
    Task<List<Order>> GetAllAsync(int skip, int take, CancellationToken ct = default);
    Task AddAsync(Order order, CancellationToken ct = default);

    /// <summary>
    /// Returns reserved orders whose <see cref="Order.ReservationExpiresAt"/> has passed the given
    /// threshold, with their items loaded. Rows are locked with SELECT ... FOR UPDATE SKIP LOCKED,
    /// so this must be called within an open transaction and concurrent callers skip locked orders.
    /// </summary>
    Task<List<Order>> GetExpiredReservationsWithLockAsync(DateTime threshold, CancellationToken ct = default);
}
