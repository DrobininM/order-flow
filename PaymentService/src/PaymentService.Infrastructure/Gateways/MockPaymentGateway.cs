using Microsoft.Extensions.Logging;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Gateways;

namespace PaymentService.Infrastructure.Gateways;

/// <summary>
/// Development stub that simulates an external payment provider with a ~90% success rate.
/// </summary>
public sealed class MockPaymentGateway : IPaymentGateway
{
    private readonly ILogger<MockPaymentGateway> _logger;

    public MockPaymentGateway(ILogger<MockPaymentGateway> logger) => _logger = logger;

    public string GatewayName => "Mock";
    public Currency Currency => Currency.USD;
    public PaymentMethod PaymentMethod => PaymentMethod.CreditCard;

    public async Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct)
    {
        _logger.LogInformation(
            "Mock gateway processing payment: {Amount} {Currency} via {Method}",
            request.Amount, request.Currency, request.PaymentMethod);

        // Simulate external API call
        await Task.Delay(200, ct);

        // Simulate ~90% success rate
        if (Random.Shared.Next(100) < 90)
        {
            return PaymentResult.Success($"mock_payment_{Guid.NewGuid():N}");
        }

        return PaymentResult.Failure("Declined by mock gateway.");
    }

    public Task<PaymentStatus> GetPaymentStatusAsync(string externalPaymentId, CancellationToken ct)
    {
        return Task.FromResult(PaymentStatus.Succeeded);
    }
}
