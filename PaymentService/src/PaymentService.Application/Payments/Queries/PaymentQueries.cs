using ErrorOr;
using MediatR;
using PaymentService.Application.Payments.DTOs;

namespace PaymentService.Application.Payments.Queries;

/// <summary>
/// Returns the payment for an order. The caller must be the payment owner.
/// </summary>
public sealed record GetPaymentQuery(Guid OrderId, Guid UserId) : IRequest<ErrorOr<PaymentDto>>;
