using ErrorOr;
using MediatR;
using OrderService.Application.Common;
using OrderService.Application.Orders.DTOs;
using OrderService.Application.Orders.Queries;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Handlers;

public sealed class GetOrdersQueryHandler : IRequestHandler<GetOrdersQuery, ErrorOr<PagedOrdersDto>>
{
    private readonly IOrderRepository _orderRepository;

    public GetOrdersQueryHandler(IOrderRepository orderRepository)
    {
        _orderRepository = orderRepository;
    }

    public async Task<ErrorOr<PagedOrdersDto>> Handle(GetOrdersQuery request, CancellationToken cancellationToken)
    {
        IEnumerable<Order> orders;

        if (request.IsAdmin)
            orders = await _orderRepository.GetAllAsync(request.Skip, request.Take, cancellationToken);
        else
            orders = await _orderRepository.GetByUserIdAsync(request.UserId, request.Skip, request.Take, cancellationToken);

        var items = orders.Select(o => o.ToListItemDto()).ToList();

        return new PagedOrdersDto(items, items.Count, request.Skip, request.Take);
    }
}
