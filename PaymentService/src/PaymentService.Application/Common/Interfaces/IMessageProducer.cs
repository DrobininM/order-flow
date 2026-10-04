namespace PaymentService.Application.Common.Interfaces;

/// <summary>
/// Abstraction over the outbound message broker (Kafka).
/// </summary>
public interface IMessageProducer
{
    /// <summary>
    /// Publishes a message to the given topic.
    /// </summary>
    Task PublishAsync(string topic, string key, string message, CancellationToken ct = default);
}
