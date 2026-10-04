using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Gateways;

namespace PaymentService.IntegrationTests;

/// <summary>
/// Deterministic gateway used by integration tests so the payment outcome does not depend
/// on the random behaviour of the development <c>MockPaymentGateway</c>.
/// </summary>
internal sealed class StubPaymentGateway : IPaymentGateway
{
    public const string ExternalPaymentId = "stub_payment_1";

    public string GatewayName => "Stub";
    public Currency Currency => Currency.USD;
    public PaymentMethod PaymentMethod => PaymentMethod.CreditCard;

    public Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct) =>
        Task.FromResult(PaymentResult.Success(ExternalPaymentId));

    public Task<PaymentStatus> GetPaymentStatusAsync(string externalPaymentId, CancellationToken ct) =>
        Task.FromResult(PaymentStatus.Succeeded);
}
