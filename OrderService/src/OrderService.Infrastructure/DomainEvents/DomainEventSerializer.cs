using System.Text.Json;
using OrderService.Domain.Common;

namespace OrderService.Infrastructure.DomainEvents;

/// <summary>
/// Serializes and deserializes domain events for durable storage in the outbox.
/// </summary>
public sealed class DomainEventSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Returns the fully-qualified type name used to resolve the event type during deserialization.
    /// </summary>
    public string GetTypeName(IDomainEvent domainEvent) =>
        domainEvent.GetType().FullName
        ?? throw new InvalidOperationException("Domain event type has no full name.");

    /// <summary>
    /// Serializes the event using its concrete runtime type.
    /// </summary>
    public string Serialize(IDomainEvent domainEvent) =>
        JsonSerializer.Serialize(domainEvent, domainEvent.GetType(), Options);

    /// <summary>
    /// Deserializes a stored payload back into a concrete <see cref="IDomainEvent"/>.
    /// </summary>
    public IDomainEvent Deserialize(string typeName, string payload)
    {
        var type = typeof(IDomainEvent).Assembly.GetType(typeName)
            ?? throw new InvalidOperationException($"Unknown domain event type '{typeName}'.");

        return JsonSerializer.Deserialize(payload, type, Options) as IDomainEvent
            ?? throw new InvalidOperationException($"Failed to deserialize domain event '{typeName}'.");
    }
}
