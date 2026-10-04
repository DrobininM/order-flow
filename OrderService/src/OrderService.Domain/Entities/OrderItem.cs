using ErrorOr;
using OrderService.Domain.Common;
using OrderService.Domain.Enums;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

/// <summary>
/// Represents an order item within an order.
/// </summary>
public sealed class OrderItem : Entity<Guid>
{
    /// <summary>
    /// ID of the parent order.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// ID of the product being ordered.
    /// </summary>
    public Guid ProductId { get; private set; }

    /// <summary>
    /// Snapshot of the product name at the time of ordering.
    /// </summary>
    public string ProductName { get; private set; } = null!;

    /// <summary>
    /// Unit price of the product at the time of ordering.
    /// </summary>
    public decimal UnitPrice { get; private set; }

    /// <summary>
    /// Currency of the price.
    /// </summary>
    public Currency Currency { get; private set; }

    /// <summary>
    /// Quantity ordered.
    /// </summary>
    public int Quantity { get; private set; }

    /// <summary>
    /// Total price for this item (unit price × quantity).
    /// </summary>
    public decimal TotalPrice => UnitPrice * Quantity;

    private OrderItem() { } // EF Core

    private OrderItem(Guid id, Guid orderId, Guid productId, string productName, decimal unitPrice,
        Currency currency, int quantity) : base(id)
    {
        OrderId = orderId;
        ProductId = productId;
        ProductName = productName;
        UnitPrice = unitPrice;
        Currency = currency;
        Quantity = quantity;
    }

    /// <summary>
    /// Creates a new order item.
    /// </summary>
    public static ErrorOr<OrderItem> Create(Guid orderId, Guid productId, string productName, Money price, int quantity)
    {
        if (string.IsNullOrWhiteSpace(productName))
            return Error.Validation(ErrorCodes.OrderItemEmptyName, "Product name cannot be empty.");

        if (quantity <= 0)
            return Error.Validation(ErrorCodes.OrderItemInvalidQuantity, "Quantity must be greater than zero.");

        return new OrderItem(
            Guid.NewGuid(),
            orderId,
            productId,
            productName,
            price.Amount,
            price.Currency,
            quantity);
    }

    /// <summary>
    /// Updates the quantity. Must be positive.
    /// </summary>
    /// <exception cref="DomainException">Thrown if quantity is zero or negative.</exception>
    public void UpdateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new DomainException(ErrorCodes.OrderItemInvalidQuantity, "Quantity must be positive.");

        Quantity = quantity;
    }
}
