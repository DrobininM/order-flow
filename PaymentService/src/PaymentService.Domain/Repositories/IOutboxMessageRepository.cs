using PaymentService.Domain.Entities;

namespace PaymentService.Domain.Repositories;

/// <summary>
/// Persistence contract for outgoing integration events (outbox).
/// </summary>
public interface IOutboxMessageRepository
{
    /// <summary>
    /// Returns unprocessed messages whose retry count does not exceed <paramref name="maxRetryCount"/>.
    /// Rows are locked with SELECT ... FOR UPDATE SKIP LOCKED, so this must be called within an
    /// open transaction and concurrent callers will skip already-locked messages.
    /// </summary>
    Task<List<OutboxMessage>> GetUnprocessedWithLockAsync(int batchSize, int maxRetryCount, CancellationToken ct = default);

    Task AddAsync(OutboxMessage message, CancellationToken ct = default);
}
