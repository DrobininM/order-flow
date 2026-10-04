using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Gateways;

/// <summary>
/// Abstraction over an external payment provider. New providers are added by implementing
/// this interface and registering the implementation in DI; the factory requires no changes.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Human-readable gateway name.
    /// </summary>
    string GatewayName { get; }

    /// <summary>
    /// Currency handled by this gateway.
    /// </summary>
    Currency Currency { get; }

    /// <summary>
    /// Payment method handled by this gateway.
    /// </summary>
    PaymentMethod PaymentMethod { get; }

    /// <summary>
    /// Sends the payment for processing.
    /// </summary>
    Task<PaymentResult> ProcessPaymentAsync(PaymentRequest request, CancellationToken ct);

    /// <summary>
    /// Returns the current status of a previously processed payment.
    /// </summary>
    Task<PaymentStatus> GetPaymentStatusAsync(string externalPaymentId, CancellationToken ct);
}
