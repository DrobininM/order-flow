using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using Polly.Retry;
using Polly.Timeout;

namespace PaymentService.Infrastructure.Resilience;

public static class ResilienceServiceExtensions
{
    public static IServiceCollection AddResiliencePolicies(this IServiceCollection services)
    {
        services.AddHttpClient(HttpClientNames.Resilient)
            .AddPolicyHandler((serviceProvider, _) => GetRetryPolicy(serviceProvider))
            .AddPolicyHandler((serviceProvider, _) => GetCircuitBreakerPolicy(serviceProvider))
            .AddPolicyHandler((serviceProvider, _) => GetTimeoutPolicy(serviceProvider));

        return services;
    }

    private static AsyncRetryPolicy<HttpResponseMessage> GetRetryPolicy(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ResilienceServiceExtensions));

        var options = serviceProvider.GetRequiredService<IOptions<ResilienceOptions>>().Value;

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(
                options.RetryCount,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (outcome, timeSpan, retryCount, _) =>
                {
                    logger.LogWarning("Retry {RetryCount} after {Delay}ms. Result: {Result}",
                        retryCount, timeSpan.TotalMilliseconds,
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                });
    }

    private static AsyncCircuitBreakerPolicy<HttpResponseMessage> GetCircuitBreakerPolicy(IServiceProvider serviceProvider)
    {
        var logger = serviceProvider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(nameof(ResilienceServiceExtensions));
        var options = serviceProvider.GetRequiredService<IOptions<ResilienceOptions>>().Value;

        return HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                options.CircuitBreakerFailureThreshold,
                TimeSpan.FromSeconds(options.CircuitBreakerBreakDurationSeconds),
                (outcome, _) =>
                {
                    logger.LogWarning("Circuit breaker opened. Reason: {Reason}",
                        outcome.Exception?.Message ?? outcome.Result?.StatusCode.ToString());
                },
                () => logger.LogInformation("Circuit breaker reset."));
    }

    private static AsyncTimeoutPolicy<HttpResponseMessage> GetTimeoutPolicy(IServiceProvider serviceProvider)
    {
        var options = serviceProvider.GetRequiredService<IOptions<ResilienceOptions>>().Value;

        return Policy.TimeoutAsync<HttpResponseMessage>(
            TimeSpan.FromSeconds(options.TimeoutSeconds));
    }
}
