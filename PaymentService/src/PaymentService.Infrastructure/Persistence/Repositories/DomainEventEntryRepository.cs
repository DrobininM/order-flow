using Microsoft.EntityFrameworkCore;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Repositories;

namespace PaymentService.Infrastructure.Persistence.Repositories;

public sealed class DomainEventEntryRepository : IDomainEventEntryRepository
{
    private readonly PaymentDbContext _dbContext;

    public DomainEventEntryRepository(PaymentDbContext dbContext) => _dbContext = dbContext;

    public async Task AddAsync(DomainEventEntry entry, CancellationToken ct = default) =>
        await _dbContext.DomainEventEntries.AddAsync(entry, ct);

    /// <summary>
    /// Claims pending entries older than <paramref name="olderThanUtc"/>. The raw SQL uses
    /// <c>FOR UPDATE SKIP LOCKED</c> so concurrent recovery instances do not redeliver the
    /// same event; the lock is held until the surrounding transaction commits.
    /// </summary>
    public async Task<List<DomainEventEntry>> GetPendingWithLockAsync(
        DateTime olderThanUtc,
        int batchSize,
        CancellationToken ct = default) =>
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
