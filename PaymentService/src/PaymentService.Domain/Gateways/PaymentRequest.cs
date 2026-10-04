using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Gateways;

/// <summary>
/// Immutable request passed to an <see cref="IPaymentGateway"/>.
/// </summary>
public sealed record PaymentRequest(
    decimal Amount,
    Currency Currency,
    PaymentMethod PaymentMethod,
    string Description,
    IReadOnlyDictionary<string, string> Metadata);
