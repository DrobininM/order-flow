namespace PaymentService.Domain.Common;

/// <summary>
/// Centralised error code constants used across the Domain and Application layers.
/// </summary>
public static class ErrorCodes
{
    public const string PaymentNotFound = "Payment.NotFound";
    public const string PaymentNotOwned = "Payment.NotOwned";
    public const string PaymentInvalidOrder = "Payment.InvalidOrder";
    public const string PaymentInvalidUser = "Payment.InvalidUser";
    public const string PaymentInvalidAmount = "Payment.InvalidAmount";
    public const string PaymentInvalidStatus = "Payment.InvalidStatus";
    public const string PaymentExternalIdEmpty = "Payment.ExternalIdEmpty";
    public const string PaymentErrorEmpty = "Payment.ErrorEmpty";

    public const string PaymentRequestInvalid = "PaymentRequest.Invalid";
    public const string PaymentRequestCurrencyInvalid = "PaymentRequest.CurrencyInvalid";

    public const string GatewayNotFound = "Gateway.NotFound";
}
