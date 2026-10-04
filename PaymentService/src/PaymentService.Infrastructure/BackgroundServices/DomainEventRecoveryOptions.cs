namespace PaymentService.Infrastructure.BackgroundServices;

/// <summary>
/// Configuration for <see cref="DomainEventRecoveryService"/>.
/// </summary>
public sealed class DomainEventRecoveryOptions
{
    public const string SectionName = "DomainEventRecovery";

    /// <summary>
    /// Age in seconds after which a pending domain event entry is considered lost and redelivered.
    /// </summary>
    public int RecoveryThresholdSeconds { get; init; } = 20;

    /// <summary>
    /// Delay in seconds between recovery scans.
    /// </summary>
    public int PollingIntervalSeconds { get; init; } = 10;
}
