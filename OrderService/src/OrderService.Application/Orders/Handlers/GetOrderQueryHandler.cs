using ErrorOr;
using MediatR;
using OrderService.Application.Common;
using OrderService.Application.Orders.DTOs;
using OrderService.Application.Orders.Queries;
using OrderService.Domain.Common;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Handlers;

public sealed class GetOrderQueryHandler : IRequestHandler<GetOrderQuery, ErrorOr<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrderQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ErrorOr<OrderDto>> Handle(GetOrderQuery request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, cancellationToken);
        
        if (order is null)
            return Error.NotFound(ErrorCodes.OrderNotFound, $"Order {request.OrderId} not found.");

        return order.ToDto();
    }
}
