using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.Persistence.Repositories;

public sealed class OrderRepository : IOrderRepository
{
    private readonly OrderDbContext _dbContext;

    public OrderRepository(OrderDbContext dbContext) => _dbContext = dbContext;

    public async Task<Order?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _dbContext.Orders.FindAsync([id], ct);

    public async Task<Order?> GetByIdWithItemsAsync(Guid id, CancellationToken ct = default) =>
        await _dbContext.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.Id == id, ct);

    public async Task<List<Order>> GetByUserIdAsync(Guid userId, int skip, int take, CancellationToken ct = default)
    {
        const int maxTake = 100;
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, maxTake);

        return await _dbContext.Orders
            .Include(o => o.Items)
            .Where(o => o.UserId == userId)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<List<Order>> GetAllAsync(int skip, int take, CancellationToken ct = default)
    {
        const int maxTake = 100;
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, maxTake);

        return await _dbContext.Orders
            .Include(o => o.Items)
            .OrderByDescending(o => o.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task AddAsync(Order order, CancellationToken ct = default) =>
        await _dbContext.Orders.AddAsync(order, ct);

    public async Task<List<Order>> GetExpiredReservationsWithLockAsync(DateTime threshold, CancellationToken ct = default)
    {
        var status = Domain.Enums.OrderStatus.Reserved.ToString();

        var orders = await _dbContext.Orders
            .FromSql($"""
                SELECT *, xmin FROM "Orders"
                WHERE "Status" = {status}
                  AND "ReservationExpiresAt" IS NOT NULL
                  AND "ReservationExpiresAt" < {threshold}
                ORDER BY "CreatedAt"
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

        if (orders.Count == 0)
            return orders;

        // Load the items separately: composing Include with a locking query is not supported.
        var orderIds = orders.Select(o => o.Id).ToList();

        await _dbContext.OrderItems
            .Where(i => orderIds.Contains(i.OrderId))
            .LoadAsync(ct);

        return orders;
    }
}
