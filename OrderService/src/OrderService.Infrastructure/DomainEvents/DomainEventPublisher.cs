using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.DomainEvents;

/// <summary>
/// Dispatches a domain event to in-process handlers and records the outcome on its outbox entry.
/// Failures are captured rather than rethrown so that a failed handler does not roll back the
/// surrounding business transaction; the recovery service retries the entry later.
/// </summary>
public sealed class DomainEventPublisher
{
    private readonly IPublisher _publisher;
    private readonly ILogger<DomainEventPublisher> _logger;

    public DomainEventPublisher(IPublisher publisher, ILogger<DomainEventPublisher> logger)
    {
        _publisher = publisher;
        _logger = logger;
    }

    public async Task DispatchAsync(IDomainEvent domainEvent, DomainEventEntry entry, CancellationToken ct)
    {
        try
        {
            await _publisher.Publish(domainEvent, ct);

            entry.MarkAsProcessed();
            _logger.LogInformation("Domain event {EventType} (entry {EntryId}) dispatched.", entry.EventType, entry.Id);
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            _logger.LogWarning(ex, "Event publishing cancelled: {EventType} (entry {EntryId}).", entry.EventType, entry.Id);
            
            throw;
        }
        catch (Exception ex)
        {
            entry.MarkAsFailed(ex.Message);
            _logger.LogError(ex, "Failed to dispatch domain event {EventType} (entry {EntryId}).", entry.EventType, entry.Id);
        }
    }
}
