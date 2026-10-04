namespace PaymentService.Infrastructure.BackgroundServices;

/// <summary>
/// Configuration for <see cref="OutboxProcessor"/>.
/// </summary>
public sealed class OutboxOptions
{
    public const string SectionName = "Outbox";

    /// <summary>
    /// Maximum number of failed processing attempts after which a message is no longer retried.
    /// </summary>
    public int MaxRetryCount { get; init; } = 5;
}
