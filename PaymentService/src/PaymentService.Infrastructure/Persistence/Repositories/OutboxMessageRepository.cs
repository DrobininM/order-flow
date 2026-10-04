using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Repositories;

namespace PaymentService.Infrastructure.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly PaymentDbContext _dbContext;

    public OutboxMessageRepository(PaymentDbContext dbContext) => _dbContext = dbContext;

    /// <summary>
    /// Claims a batch of pending messages. The raw SQL uses <c>FOR UPDATE SKIP LOCKED</c> so
    /// concurrent outbox processors never publish the same message; the lock remains held for
    /// the lifetime of the surrounding transaction (see <c>OutboxProcessor</c>).
    /// </summary>
    public async Task<List<OutboxMessage>> GetUnprocessedWithLockAsync(
        int batchSize,
        int maxRetryCount,
        CancellationToken ct = default) =>
        await _dbContext.OutboxMessages
            .FromSql($"""
                SELECT * FROM "OutboxMessages"
                WHERE "ProcessedAt" IS NULL AND "RetryCount" <= {maxRetryCount}
                ORDER BY "CreatedAt"
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);

    public async Task AddAsync(OutboxMessage message, CancellationToken ct = default) =>
        await _dbContext.OutboxMessages.AddAsync(message, ct);
}
