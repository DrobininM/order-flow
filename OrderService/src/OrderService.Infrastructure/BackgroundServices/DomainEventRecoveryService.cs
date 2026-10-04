using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.DomainEvents;
using OrderService.Infrastructure.Persistence;

namespace OrderService.Infrastructure.BackgroundServices;

/// <summary>
/// Redelivers domain events whose outbox entries were never marked as processed
/// (for example, because the process crashed between persisting the entry and dispatching it).
/// </summary>
public sealed class DomainEventRecoveryService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<DomainEventRecoveryService> _logger;
    private readonly DomainEventRecoveryOptions _options;
    private const int BatchSize = 20;

    public DomainEventRecoveryService(
        IServiceScopeFactory scopeFactory,
        ILogger<DomainEventRecoveryService> logger,
        IOptions<DomainEventRecoveryOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("DomainEventRecoveryService started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RecoverAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error recovering domain events.");
            }

            await Task.Delay(TimeSpan.FromSeconds(_options.PollingIntervalSeconds), stoppingToken);
        }
    }

    private async Task RecoverAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IDomainEventEntryRepository>();
        var serializer = scope.ServiceProvider.GetRequiredService<DomainEventSerializer>();
        var publisher = scope.ServiceProvider.GetRequiredService<DomainEventPublisher>();
        var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

        var olderThan = DateTime.UtcNow.Subtract(TimeSpan.FromSeconds(_options.RecoveryThresholdSeconds));

        // Hold an explicit transaction so the FOR UPDATE SKIP LOCKED locks taken by
        // GetPendingAsync are kept for the duration of the dispatch, preventing other
        // instances from redelivering the same entries concurrently.
        await using var transaction = await dbContext.Database.BeginTransactionAsync(ct);

        var entries = await repository.GetPendingWithLockAsync(olderThan, BatchSize, ct);

        foreach (var entry in entries)
        {
            try
            {
                var domainEvent = serializer.Deserialize(entry.EventType, entry.Payload);
                await publisher.DispatchAsync(domainEvent, entry, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                entry.MarkAsFailed(ex.Message);
                _logger.LogError(ex, "Failed to recover domain event {EventType} (entry {EntryId}).", entry.EventType, entry.Id);
            }
        }

        if (entries.Count > 0)
            await dbContext.SaveChangesAsync(ct);

        await transaction.CommitAsync(ct);
    }
}
