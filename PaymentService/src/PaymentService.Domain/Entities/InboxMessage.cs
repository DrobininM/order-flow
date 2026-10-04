using PaymentService.Domain.Common;

namespace PaymentService.Domain.Entities;

/// <summary>
/// Represents an inbound integration message processed by this service.
/// Used to guarantee idempotent consumption of Kafka messages.
/// </summary>
public sealed class InboxMessage : Entity<Guid>
{
    /// <summary>
    /// Stable message identifier (Kafka message key or a topic/partition/offset fallback).
    /// </summary>
    public string MessageId { get; private set; } = null!;

    /// <summary>
    /// Event/topic type name (e.g. "PaymentRequestedIntegrationEvent").
    /// </summary>
    public string Type { get; private set; } = null!;

    /// <summary>
    /// Raw message payload (JSON).
    /// </summary>
    public string Payload { get; private set; } = null!;

    /// <summary>
    /// When the message was received.
    /// </summary>
    public DateTime ReceivedAt { get; private set; }

    /// <summary>
    /// When the message was successfully processed, or null if pending.
    /// </summary>
    public DateTime? ProcessedAt { get; private set; }

    private InboxMessage() { } // EF Core

    private InboxMessage(Guid id, string messageId, string type, string payload) : base(id)
    {
        MessageId = messageId;
        Type = type;
        Payload = payload;
        ReceivedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new inbox message.
    /// </summary>
    public static InboxMessage Create(string messageId, string type, string payload) =>
        new(Guid.NewGuid(), messageId, type, payload);

    /// <summary>
    /// Marks the message as successfully processed. Idempotent.
    /// </summary>
    public void MarkAsProcessed()
    {
        if (ProcessedAt.HasValue)
            return;

        ProcessedAt = DateTime.UtcNow;
    }
}
