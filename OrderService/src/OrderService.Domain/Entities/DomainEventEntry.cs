using OrderService.Domain.Common;

namespace OrderService.Domain.Entities;

/// <summary>
/// Represents a persisted domain event that must be dispatched to in-process handlers.
/// The entry is written in the same transaction as the business change and is marked as
/// processed once its event has been successfully dispatched.
/// </summary>
public sealed class DomainEventEntry : Entity<Guid>
{
    /// <summary>
    /// Fully-qualified CLR type name of the domain event (used for deserialization).
    /// </summary>
    public string EventType { get; private set; } = null!;

    /// <summary>
    /// Serialized event payload (JSON).
    /// </summary>
    public string Payload { get; private set; } = null!;

    /// <summary>
    /// When the original domain event occurred.
    /// </summary>
    public DateTime OccurredOn { get; private set; }

    /// <summary>
    /// When the entry was recorded.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the event was successfully dispatched, or null if pending.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Error message from the last failed dispatch attempt.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Number of dispatch attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    private DomainEventEntry() { } // EF Core

    private DomainEventEntry(Guid id, string eventType, string payload, DateTime occurredOn) : base(id)
    {
        EventType = eventType;
        Payload = payload;
        OccurredOn = occurredOn;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new domain event entry.
    /// </summary>
    public static DomainEventEntry Create(string eventType, string payload, DateTime occurredOn) =>
        new(Guid.NewGuid(), eventType, payload, occurredOn);

    /// <summary>
    /// Marks the entry as successfully dispatched.
    /// </summary>
    public void MarkAsProcessed()
    {
        if (ProcessedAt.HasValue)
            return;

        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed dispatch attempt.
    /// </summary>
    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
