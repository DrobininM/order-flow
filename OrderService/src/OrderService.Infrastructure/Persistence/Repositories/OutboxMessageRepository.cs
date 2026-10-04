using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.Persistence.Repositories;

public sealed class OutboxMessageRepository : IOutboxMessageRepository
{
    private readonly OrderDbContext _dbContext;

    public OutboxMessageRepository(OrderDbContext dbContext) => _dbContext = dbContext;

    public async Task<List<OutboxMessage>> GetUnprocessedWithLockAsync(int batchSize, int maxRetryCount, CancellationToken ct = default) =>
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
