using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Gateways;

/// <summary>
/// Resolves the <see cref="IPaymentGateway"/> responsible for a currency/method pair.
/// </summary>
public interface IPaymentGatewayFactory
{
    /// <summary>
    /// Returns the gateway registered for the given currency and method.
    /// </summary>
    /// <exception cref="Common.DomainException">Thrown if no gateway is registered for the pair.</exception>
    IPaymentGateway GetGateway(Currency currency, PaymentMethod method);
}
