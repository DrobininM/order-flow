using OrderService.Domain.Common;

namespace OrderService.Domain.Entities;

/// <summary>
/// Represents an outbox message for reliable event delivery.
/// </summary>
public sealed class OutboxMessage : Entity<Guid>
{
    /// <summary>
    /// Event type name (e.g. "OrderCreatedIntegrationEvent").
    /// </summary>
    public string Type { get; private set; } = null!;

    /// <summary>
    /// Serialized event payload (JSON).
    /// </summary>
    public string Payload { get; private set; } = null!;

    /// <summary>
    /// When the message was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the message was successfully processed, or null if pending.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Error message from the last failed processing attempt.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Number of retry attempts.
    /// </summary>
    public int RetryCount { get; private set; }

    private OutboxMessage() { } // EF Core

    private OutboxMessage(Guid id, string type, string payload) : base(id)
    {
        Type = type;
        Payload = payload;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new outbox message.
    /// </summary>
    public static OutboxMessage Create(string type, string payload)
    {
        return new OutboxMessage(Guid.NewGuid(), type, payload);
    }

    /// <summary>
    /// Marks the message as successfully processed.
    /// </summary>
    public void MarkAsProcessed()
    {
        if (ProcessedAt.HasValue)
            return;

        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed processing attempt.
    /// </summary>
    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
