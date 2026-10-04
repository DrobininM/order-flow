using PaymentService.Application.IntegrationEvents;

namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// Maps integration event type names to Kafka topic names.
/// </summary>
public sealed class KafkaMessageRouter
{
    private static readonly Dictionary<string, string> TopicMap = new()
    {
        [nameof(PaymentSucceededIntegrationEvent)] = KafkaTopicNames.PaymentSucceeded,
        [nameof(PaymentFailedIntegrationEvent)] = KafkaTopicNames.PaymentFailed
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
