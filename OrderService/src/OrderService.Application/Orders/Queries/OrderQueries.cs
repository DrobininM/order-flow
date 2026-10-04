using ErrorOr;
using MediatR;
using OrderService.Application.Orders.DTOs;

namespace OrderService.Application.Orders.Queries;

public sealed record GetOrderQuery(Guid OrderId) : IRequest<ErrorOr<OrderDto>>;

public sealed record GetOrdersQuery(Guid UserId, bool IsAdmin, int Skip = 0, int Take = 20) : IRequest<ErrorOr<PagedOrdersDto>>;
