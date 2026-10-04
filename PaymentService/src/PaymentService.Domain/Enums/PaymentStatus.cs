namespace PaymentService.Domain.Enums;

/// <summary>
/// Lifecycle states of a payment.
/// </summary>
public enum PaymentStatus
{
    Pending = 1,
    Processing = 2,
    Succeeded = 3,
    Failed = 4,
    Refunded = 5
}
