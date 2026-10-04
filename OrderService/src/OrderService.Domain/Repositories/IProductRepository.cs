using OrderService.Domain.Entities;

namespace OrderService.Domain.Repositories;

public interface IProductRepository
{
    Task<Product?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<List<Product>> GetByIdsAsync(IEnumerable<Guid> ids, CancellationToken ct = default);

    /// <summary>
    /// Returns products by IDs with an exclusive row-level lock (SELECT ... FOR UPDATE).
    /// Used when reserving or releasing stock to prevent race conditions.
    /// Must be called within an open transaction, otherwise the lock is released immediately.
    /// </summary>
    Task<List<Product>> GetByIdsWithLockAsync(IReadOnlyList<Guid> ids, CancellationToken ct = default);

    Task<List<Product>> GetAvailableAsync(int skip, int take, CancellationToken ct = default);
    Task<int> GetTotalCountAsync(CancellationToken ct = default);
    Task AddAsync(Product product, CancellationToken ct = default);
    void Delete(Product product);

    /// <summary>
    /// Returns a product by ID with an exclusive row-level lock (SELECT ... FOR UPDATE).
    /// Must be called within an open transaction, otherwise the lock is released immediately.
    /// </summary>
    Task<Product?> GetByIdWithLockAsync(Guid id, CancellationToken ct = default);
}
