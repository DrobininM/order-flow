namespace PaymentService.Domain.Entities;

/// <summary>
/// Outcome of a payment processed by an <see cref="Gateways.IPaymentGateway"/>.
/// </summary>
public sealed class PaymentResult
{
    /// <summary>
    /// Whether the gateway accepted the payment.
    /// </summary>
    public bool IsSuccess { get; }

    /// <summary>
    /// Gateway identifier returned on success, or null on failure.
    /// </summary>
    public string? ExternalPaymentId { get; }

    /// <summary>
    /// Failure reason returned on failure, or null on success.
    /// </summary>
    public string? ErrorMessage { get; }

    private PaymentResult(bool isSuccess, string? externalPaymentId, string? errorMessage)
    {
        IsSuccess = isSuccess;
        ExternalPaymentId = externalPaymentId;
        ErrorMessage = errorMessage;
    }

    /// <summary>
    /// Creates a successful result.
    /// </summary>
    public static PaymentResult Success(string externalPaymentId) =>
        new(true, externalPaymentId, null);

    /// <summary>
    /// Creates a failed result with the gateway error.
    /// </summary>
    public static PaymentResult Failure(string errorMessage) =>
        new(false, null, errorMessage);
}
