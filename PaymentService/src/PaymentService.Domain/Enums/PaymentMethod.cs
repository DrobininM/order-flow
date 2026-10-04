namespace PaymentService.Domain.Enums;

/// <summary>
/// Supported payment methods.
/// </summary>
public enum PaymentMethod
{
    CreditCard = 1,
    DebitCard = 2,
    PayPal = 3,
    Crypto = 4
}
