using PaymentService.Application.Payments.DTOs;
using PaymentService.Domain.Entities;

namespace PaymentService.Application.Common;

/// <summary>
/// Maps domain entities to application DTOs.
/// </summary>
public static class MappingExtensions
{
    public static PaymentDto ToDto(this Payment payment) =>
        new(
            payment.Id,
            payment.OrderId,
            payment.UserId,
            payment.Amount,
            payment.Currency.ToString(),
            payment.Method.ToString(),
            payment.Status.ToString(),
            payment.ExternalPaymentId,
            payment.ErrorMessage,
            payment.CreatedAt,
            payment.CompletedAt);
}
