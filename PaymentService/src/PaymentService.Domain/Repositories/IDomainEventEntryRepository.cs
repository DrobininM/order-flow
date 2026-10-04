using PaymentService.Domain.Entities;

namespace PaymentService.Domain.Repositories;

/// <summary>
/// Persistence contract for domain event outbox entries.
/// </summary>
public interface IDomainEventEntryRepository
{
    /// <summary>
    /// Adds a new pending domain event entry.
    /// </summary>
    Task AddAsync(DomainEventEntry entry, CancellationToken ct = default);

    /// <summary>
    /// Returns pending entries older than <paramref name="olderThanUtc"/> that require redelivery.
    /// Rows are locked with SELECT ... FOR UPDATE SKIP LOCKED, so this must be called within an
    /// open transaction and concurrent callers will skip already-locked entries.
    /// </summary>
    Task<List<DomainEventEntry>> GetPendingWithLockAsync(DateTime olderThanUtc, int batchSize, CancellationToken ct = default);
}
