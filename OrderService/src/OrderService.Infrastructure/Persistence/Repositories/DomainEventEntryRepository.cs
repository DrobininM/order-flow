using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.Persistence.Repositories;

public sealed class DomainEventEntryRepository : IDomainEventEntryRepository
{
    private readonly OrderDbContext _dbContext;

    public DomainEventEntryRepository(OrderDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(DomainEventEntry entry, CancellationToken ct = default) =>
        await _dbContext.DomainEventEntries.AddAsync(entry, ct);

    public async Task<List<DomainEventEntry>> GetPendingWithLockAsync(DateTime olderThanUtc, int batchSize, CancellationToken ct = default) =>
        await _dbContext.DomainEventEntries
            .FromSql($"""
                SELECT * FROM "DomainEventEntries"
                WHERE "ProcessedAt" IS NULL AND "CreatedAt" < {olderThanUtc}
                ORDER BY "CreatedAt"
                LIMIT {batchSize}
                FOR UPDATE SKIP LOCKED
                """)
            .ToListAsync(ct);
}
