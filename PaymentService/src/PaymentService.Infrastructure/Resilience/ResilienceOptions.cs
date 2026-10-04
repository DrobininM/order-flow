namespace PaymentService.Infrastructure.Resilience;

/// <summary>
/// Configuration for Polly resilience policies.
/// </summary>
public sealed class ResilienceOptions
{
    public const string SectionName = "Resilience";

    public int RetryCount { get; init; } = 3;

    public int CircuitBreakerFailureThreshold { get; init; } = 5;

    public int CircuitBreakerBreakDurationSeconds { get; init; } = 30;

    public int TimeoutSeconds { get; init; } = 10;
}
