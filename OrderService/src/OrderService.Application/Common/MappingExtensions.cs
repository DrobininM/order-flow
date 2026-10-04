using OrderService.Application.Orders.DTOs;
using OrderService.Application.Products.DTOs;
using OrderService.Domain.Entities;

namespace OrderService.Application.Common;

public static class MappingExtensions
{
    public static OrderDto ToDto(this Order order)
    {
        var total = order.GetTotalAmount();
        return new OrderDto(
            order.Id,
            order.UserId,
            order.Status.ToString(),
            total.Amount,
            total.Currency.ToString(),
            order.Items.Select(i => i.ToDto()).ToList(),
            order.CreatedAt,
            order.UpdatedAt);
    }

    public static OrderItemDto ToDto(this OrderItem item) =>
        new(
            item.ProductId,
            item.ProductName,
            item.UnitPrice,
            item.Currency.ToString(),
            item.Quantity,
            item.TotalPrice);

    public static OrderListItemDto ToListItemDto(this Order order)
    {
        var total = order.GetTotalAmount();
        return new OrderListItemDto(
            order.Id,
            order.UserId,
            order.Status.ToString(),
            total.Amount,
            total.Currency.ToString(),
            order.Items.Count,
            order.CreatedAt);
    }

    public static ProductDto ToDto(this Product product) =>
        new(
            product.Id,
            product.Name,
            product.Description,
            product.Price.Amount,
            product.Price.Currency.ToString(),
            product.StockQuantity,
            product.AvailableQuantity,
            product.IsAvailable,
            product.CreatedAt);

    public static IntegrationEvents.OrderItemDto ToIntegrationDto(this OrderItem item) =>
        new(item.ProductId, item.ProductName, item.Quantity, item.UnitPrice);
}
