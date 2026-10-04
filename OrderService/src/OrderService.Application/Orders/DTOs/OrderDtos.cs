using OrderService.Application.Orders.Commands;

namespace OrderService.Application.Orders.DTOs;

/// <summary>
/// Request body for creating an order. The user is resolved from the authenticated principal.
/// </summary>
public sealed record CreateOrderRequest(IReadOnlyList<CreateOrderItem> Items);

/// <summary>
/// Request body for adding an item to an order. The order id comes from the route.
/// </summary>
public sealed record AddOrderItemRequest(Guid ProductId, int Quantity);

public sealed record OrderDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    string Currency,
    IReadOnlyList<OrderItemDto> Items,
    DateTime CreatedAt,
    DateTime UpdatedAt);

public sealed record OrderItemDto(
    Guid ProductId,
    string ProductName,
    decimal UnitPrice,
    string Currency,
    int Quantity,
    decimal TotalPrice);

public sealed record OrderListItemDto(
    Guid Id,
    Guid UserId,
    string Status,
    decimal TotalAmount,
    string Currency,
    int ItemCount,
    DateTime CreatedAt);

public sealed record PagedOrdersDto(IReadOnlyList<OrderListItemDto> Items, int TotalCount, int Skip, int Take);
