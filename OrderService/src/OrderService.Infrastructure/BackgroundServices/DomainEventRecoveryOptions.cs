namespace OrderService.Infrastructure.BackgroundServices;

/// <summary>
/// Configuration for <see cref="DomainEventRecoveryService"/>.
/// </summary>
public sealed class DomainEventRecoveryOptions
{
    public const string SectionName = "DomainEventRecovery";

    /// <summary>
    /// Events that remain unprocessed for longer than this many seconds are redelivered.
    /// </summary>
    public int RecoveryThresholdSeconds { get; init; } = 20;

    /// <summary>
    /// Delay in seconds between recovery scans.
    /// </summary>
    public int PollingIntervalSeconds { get; init; } = 10;
}
