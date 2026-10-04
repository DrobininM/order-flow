using PaymentService.Domain.Common;

namespace PaymentService.Domain.Entities;

/// <summary>
/// Represents an outbox message for reliable delivery of integration events to Kafka.
/// </summary>
public sealed class OutboxMessage : Entity<Guid>
{
    /// <summary>
    /// Integration event type name (e.g. "PaymentSucceededIntegrationEvent").
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
    /// When the message was successfully published, or null if pending.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    /// <summary>
    /// Error message from the last failed publish attempt.
    /// </summary>
    public string? Error { get; private set; }

    /// <summary>
    /// Number of publish attempts.
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
    public static OutboxMessage Create(string type, string payload) =>
        new(Guid.NewGuid(), type, payload);

    /// <summary>
    /// Marks the message as successfully published. Idempotent.
    /// </summary>
    public void MarkAsProcessed()
    {
        if (ProcessedAt.HasValue)
            return;

        ProcessedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed publish attempt.
    /// </summary>
    public void MarkAsFailed(string error)
    {
        Error = error;
        RetryCount++;
    }
}
