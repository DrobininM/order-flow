namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// Centralised Kafka topic name constants.
/// </summary>
public static class KafkaTopicNames
{
    public const string PaymentRequested = "payment.requested";
    public const string PaymentSucceeded = "payment.succeeded";
    public const string PaymentFailed = "payment.failed";
}
