using ErrorOr;
using MediatR;
using OrderService.Application.Orders.DTOs;

namespace OrderService.Application.Orders.Commands;

public sealed record CreateOrderCommand(Guid UserId, IReadOnlyList<CreateOrderItem> Items) : IRequest<ErrorOr<OrderDto>>;

public sealed record CreateOrderItem(Guid ProductId, int Quantity);

public sealed record AddOrderItemCommand(Guid OrderId, Guid ProductId, int Quantity) : IRequest<ErrorOr<OrderDto>>;

public sealed record PayOrderCommand(Guid OrderId, Guid UserId) : IRequest<ErrorOr<OrderDto>>;

public sealed record CancelOrderCommand(Guid OrderId, Guid UserId, string? Reason = null) : IRequest<ErrorOr<OrderDto>>;
