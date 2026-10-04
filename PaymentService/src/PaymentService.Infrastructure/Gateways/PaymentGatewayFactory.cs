using PaymentService.Domain.Common;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Gateways;

namespace PaymentService.Infrastructure.Gateways;

/// <summary>
/// Selects a gateway from all registered <see cref="IPaymentGateway"/> implementations
/// based on the requested currency and payment method.
/// </summary>
public sealed class PaymentGatewayFactory : IPaymentGatewayFactory
{
    private readonly IEnumerable<IPaymentGateway> _gateways;

    public PaymentGatewayFactory(IEnumerable<IPaymentGateway> gateways) => _gateways = gateways;

    public IPaymentGateway GetGateway(Currency currency, PaymentMethod method)
    {
        var gateway = _gateways.FirstOrDefault(g =>
            g.Currency == currency && g.PaymentMethod == method);

        if (gateway is null)
            throw new DomainException(
                ErrorCodes.GatewayNotFound,
                $"No gateway is registered for currency '{currency}' and method '{method}'.");

        return gateway;
    }
}
