using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.Persistence.Repositories;

public sealed class ProductRepository : IProductRepository
{
    private readonly OrderDbContext _dbContext;

    public ProductRepository(OrderDbContext dbContext) => _dbContext = dbContext;

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _dbContext.Products.FindAsync([id], ct);

    public async Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default) =>
        await _dbContext.Products.Where(p => ids.Contains(p.Id)).ToListAsync(ct);

    /// <summary>
    /// Returns products by IDs with an exclusive row-level lock (SELECT ... FOR UPDATE).
    /// The caller must invoke this within an open transaction (BeginTransactionAsync),
    /// otherwise the lock is released as soon as the statement completes.
    /// </summary>
    public async Task<List<Product>> GetByIdsWithLockAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default)
    {
        if (ids.Count == 0)
            return [];

        return await _dbContext.Products
            .FromSql($"SELECT *, xmin FROM \"Products\" WHERE \"Id\" = ANY({ids}) ORDER BY \"Id\" FOR UPDATE")
            .ToListAsync(ct);
    }

    public async Task<List<Product>> GetAvailableAsync(int skip, int take, CancellationToken ct = default)
    {
        const int maxTake = 100;
        skip = Math.Max(0, skip);
        take = Math.Clamp(take, 1, maxTake);

        return await _dbContext.Products
            .Where(p => p.IsAvailable)
            .OrderBy(p => p.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(ct);
    }

    public async Task<int> GetTotalCountAsync(CancellationToken ct = default) =>
        await _dbContext.Products.CountAsync(p => p.IsAvailable, ct);

    public async Task AddAsync(Product product, CancellationToken ct = default) =>
        await _dbContext.Products.AddAsync(product, ct);

    public void Delete(Product product) => _dbContext.Products.Remove(product);

    public async Task<Product?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default) =>
        (await GetByIdsWithLockAsync([id], ct)).FirstOrDefault();
}
