using ErrorOr;
using MediatR;
using PaymentService.Application.Common;
using PaymentService.Application.Payments.DTOs;
using PaymentService.Application.Payments.Queries;
using PaymentService.Domain.Common;
using PaymentService.Domain.Repositories;

namespace PaymentService.Application.Payments.Handlers;

public sealed class GetPaymentQueryHandler : IRequestHandler<GetPaymentQuery, ErrorOr<PaymentDto>>
{
    private readonly IPaymentRepository _paymentRepository;

    public GetPaymentQueryHandler(IPaymentRepository paymentRepository)
    {
        _paymentRepository = paymentRepository;
    }

    public async Task<ErrorOr<PaymentDto>> Handle(GetPaymentQuery request, CancellationToken cancellationToken)
    {
        var payment = await _paymentRepository.GetByOrderIdAsync(request.OrderId, cancellationToken);

        if (payment is null)
            return Error.NotFound(ErrorCodes.PaymentNotFound, "Payment not found.");

        if (payment.UserId != request.UserId)
            return Error.Forbidden(ErrorCodes.PaymentNotOwned, "Payments can be viewed only by their owners.");

        return payment.ToDto();
    }
}
