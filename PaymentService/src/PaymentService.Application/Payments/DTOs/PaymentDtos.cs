namespace PaymentService.Application.Payments.DTOs;

/// <summary>
/// Read model returned to clients for a single payment.
/// </summary>
public sealed record PaymentDto(
    Guid Id,
    Guid OrderId,
    Guid UserId,
    decimal Amount,
    string Currency,
    string Method,
    string Status,
    string? ExternalPaymentId,
    string? ErrorMessage,
    DateTime CreatedAt,
    DateTime? CompletedAt);
