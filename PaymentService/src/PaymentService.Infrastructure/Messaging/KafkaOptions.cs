namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// Configuration options for the Kafka connection.
/// </summary>
public sealed class KafkaOptions
{
    public const string SectionName = "Kafka";

    public string BootstrapServers { get; set; } = string.Empty;

    /// <summary>
    /// Consumer group identifier used when consuming inbound integration events.
    /// </summary>
    public string GroupId { get; set; } = "payment-service";
}
