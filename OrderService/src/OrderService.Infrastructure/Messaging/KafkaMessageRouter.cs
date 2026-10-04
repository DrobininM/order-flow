using OrderService.Application.IntegrationEvents;

namespace OrderService.Infrastructure.Messaging;

/// <summary>
/// Maps integration event type names to Kafka topic names.
/// </summary>
public sealed class KafkaMessageRouter
{
    private static readonly Dictionary<string, string> TopicMap = new()
    {
        [nameof(OrderCreatedIntegrationEvent)] = KafkaTopicNames.OrderCreated,
        [nameof(OrderCancelledIntegrationEvent)] = KafkaTopicNames.OrderCancelled,
        [nameof(PaymentRequestedIntegrationEvent)] = KafkaTopicNames.PaymentRequested
    };

    /// <summary>
    /// Returns the Kafka topic for the given event type name.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if no mapping exists for the type.</exception>
    public string GetDestinationTopic(string eventType)
    {
        if (TopicMap.TryGetValue(eventType, out var topic))
            return topic;

        throw new InvalidOperationException($"Event mapping '{eventType}' not registered.");
    }
}

/// <summary>
/// Centralised Kafka topic name constants.
/// </summary>
public static class KafkaTopicNames
{
    public const string OrderCreated = "order.created";
    public const string OrderCancelled = "order.cancelled";
    public const string PaymentRequested = "payment.requested";
    public const string PaymentSucceeded = "payment.succeeded";
    public const string PaymentFailed = "payment.failed";
}
