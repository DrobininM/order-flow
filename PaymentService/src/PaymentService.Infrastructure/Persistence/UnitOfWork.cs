using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Common;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Repositories;
using PaymentService.Infrastructure.DomainEvents;

namespace PaymentService.Infrastructure.Persistence;

/// <summary>
/// Coordinates persistence across multiple repositories.
/// Domain events are persisted as outbox entries in the same transaction as the business
/// change, then dispatched to in-process handlers after the save succeeds. Entries are
/// marked as processed on success; failures are retried by the DomainEventRecoveryService.
/// </summary>
public sealed class UnitOfWork : IUnitOfWork
{
    private readonly PaymentDbContext _dbContext;
    private readonly IDomainEventEntryRepository _domainEventRepository;
    private readonly DomainEventPublisher _domainEventPublisher;
    private readonly DomainEventSerializer _domainEventSerializer;
    private readonly List<(IDomainEvent DomainEvent, DomainEventEntry Entry)> _pendingDomainEvents = [];

    public UnitOfWork(
        PaymentDbContext dbContext,
        IDomainEventEntryRepository domainEventRepository,
        DomainEventPublisher domainEventPublisher,
        DomainEventSerializer domainEventSerializer)
    {
        _dbContext = dbContext;
        _domainEventRepository = domainEventRepository;
        _domainEventPublisher = domainEventPublisher;
        _domainEventSerializer = domainEventSerializer;
    }

    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var domainEvents = DequeueDomainEvents();
        var entries = new DomainEventEntry[domainEvents.Count];

        foreach (var (domainEvent, i) in domainEvents.Select((domainEvent, i) => (domainEvent, i)))
        {
            var entry = DomainEventEntry.Create(
                _domainEventSerializer.GetTypeName(domainEvent),
                _domainEventSerializer.Serialize(domainEvent),
                domainEvent.OccurredOn);

            await _domainEventRepository.AddAsync(entry, ct);
            entries[i] = entry;
        }

        // Persist the business changes and the domain event entries atomically.
        var result = await _dbContext.SaveChangesAsync(ct);

        if (entries.Length == 0)
            return result;

        // Within an explicit transaction, defer dispatching until commit: handlers must not
        // observe uncommitted changes, and a rollback must not leave events already dispatched.
        if (_dbContext.Database.CurrentTransaction is not null)
        {
            for (var i = 0; i < domainEvents.Count; i++)
                _pendingDomainEvents.Add((domainEvents[i], entries[i]));

            return result;
        }

        await DispatchDomainEventsAsync(domainEvents, entries, ct);

        return result;
    }

    public Task BeginTransactionAsync(CancellationToken ct = default) =>
        _dbContext.Database.BeginTransactionAsync(ct);

    public async Task CommitTransactionAsync(CancellationToken ct = default)
    {
        await _dbContext.Database.CommitTransactionAsync(ct);

        if (_pendingDomainEvents.Count == 0)
            return;

        var pending = _pendingDomainEvents.ToList();
        _pendingDomainEvents.Clear();

        for (var i = 0; i < pending.Count; i++)
        {
            await _domainEventPublisher.DispatchAsync(pending[i].DomainEvent, pending[i].Entry, ct);
        }

        // Persist the ProcessedAt / Error / RetryCount marks.
        await _dbContext.SaveChangesAsync(ct);
    }

    public async Task RollbackTransactionAsync(CancellationToken ct = default)
    {
        _pendingDomainEvents.Clear();
        await _dbContext.Database.RollbackTransactionAsync(ct);
    }

    private async Task DispatchDomainEventsAsync(
        List<IDomainEvent> domainEvents,
        DomainEventEntry[] entries,
        CancellationToken ct)
    {
        for (var i = 0; i < domainEvents.Count; i++)
        {
            await _domainEventPublisher.DispatchAsync(domainEvents[i], entries[i], ct);
        }

        // Persist the ProcessedAt / Error / RetryCount marks from Dispatch.
        await _dbContext.SaveChangesAsync(ct);
    }

    private List<IDomainEvent> DequeueDomainEvents() =>
        _dbContext.ChangeTracker
            .Entries<object>()
            .Select(e => e.Entity)
            .OfType<IAggregateRoot>()
            .SelectMany(aggregateRoot => aggregateRoot.DequeueDomainEvents())
            .ToList();
}
