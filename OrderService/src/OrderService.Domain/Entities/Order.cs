using OrderService.Domain.Common;
using OrderService.Domain.DomainEvents;
using OrderService.Domain.Enums;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

/// <summary>
/// Represents a customer order.
/// </summary>
public sealed class Order : AggregateRoot<Guid>
{
    private readonly List<OrderItem> _items = [];

    /// <summary>
    /// ID of the user who placed the order.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Current order status.
    /// </summary>
    public OrderStatus Status { get; private set; }

    /// <summary>
    /// Date and time when the order was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the order was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// When the reservation expires (only set when status is Reserved).
    /// </summary>
    public DateTime? ReservationExpiresAt { get; private set; }

    /// <summary>
    /// Order items.
    /// </summary>
    public IReadOnlyList<OrderItem> Items => _items.AsReadOnly();

    /// <summary>
    /// Optimistic concurrency version.
    /// </summary>
    public uint Version { get; private set; }

    private Order() { } // EF Core

    private Order(Guid id, Guid userId) : base(id)
    {
        UserId = userId;
        Status = OrderStatus.Draft;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Factory method to create a new draft order.
    /// </summary>
    public static Order Create(Guid userId)
    {
        var order = new Order(Guid.NewGuid(), userId);
        order.AddDomainEvent(new OrderCreatedDomainEvent(order.Id, order.UserId, order.CreatedAt));

        return order;
    }

    /// <summary>
    /// Adds an item to the order. Only allowed in Draft status.
    /// If the product already exists in the order, the quantity is increased.
    /// </summary>
    /// <param name="productId">ID of the product.</param>
    /// <param name="productName">Name of the product (snapshot).</param>
    /// <param name="price">Unit price.</param>
    /// <param name="quantity">Quantity to add. Must be positive.</param>
    /// <returns>The created or updated order item.</returns>
    /// <exception cref="DomainException">
    /// Thrown if the order is not in Draft status, or if the item data is invalid.
    /// </exception>
    public OrderItem AddItem(Guid productId, string productName, Money price, int quantity)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException(ErrorCodes.OrderInvalidStatus, "Cannot add items to a non-draft order.");

        var existingItem = _items.FirstOrDefault(i => i.ProductId == productId);

        if (existingItem is not null)
        {
            existingItem.UpdateQuantity(existingItem.Quantity + quantity);
            UpdateTimestamp();

            return existingItem;
        }

        var createResult = OrderItem.Create(Id, productId, productName, price, quantity);
        if (createResult.IsError)
            throw new DomainException(createResult.FirstError.Code, createResult.FirstError.Description);

        _items.Add(createResult.Value);
        UpdateTimestamp();

        return createResult.Value;
    }

    /// <summary>
    /// Removes an item from the order by product ID. Only allowed in Draft status.
    /// If the item does not exist, the call is a no-op.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the order is not in Draft status.</exception>
    public void RemoveItem(Guid productId)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException(ErrorCodes.OrderInvalidStatus, "Cannot remove items from a non-draft order.");

        var item = _items.FirstOrDefault(i => i.ProductId == productId);

        if (item is null)
        {
            return;
        }

        _items.Remove(item);
        UpdateTimestamp();
    }

    /// <summary>
    /// Calculates the total amount of all items.
    /// </summary>
    public Money GetTotalAmount()
    {
        if (_items.Count == 0)
            return Money.Zero(Currency.USD);

        var currency = _items[0].Currency;
        var total = _items.Sum(i => i.TotalPrice);

        return new Money(total, currency);
    }

    /// <summary>
    /// Marks the order as Reserved. Only allowed from Draft status.
    /// Sets the reservation expiration time.
    /// </summary>
    /// <param name="reservationExpiresAt">When the reservation should expire.</param>
    /// <exception cref="DomainException">Thrown if the order is not in Draft status.</exception>
    public void MarkAsReserved(DateTime reservationExpiresAt)
    {
        if (Status != OrderStatus.Draft)
            throw new DomainException(ErrorCodes.OrderInvalidStatus, "Only draft orders can be reserved.");
        
        Status = OrderStatus.Reserved;
        ReservationExpiresAt = reservationExpiresAt;
        AddDomainEvent(new OrderReservedDomainEvent(Id, UserId, DateTime.UtcNow));
        
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the order as Paid. Only allowed from Reserved status.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the order is not in Reserved status.</exception>
    public void MarkAsPaid()
    {
        if (Status != OrderStatus.Reserved)
            throw new DomainException(ErrorCodes.OrderInvalidStatus, "Only reserved orders can be paid.");

        Status = OrderStatus.Paid;
        ReservationExpiresAt = null;
        AddDomainEvent(new OrderPaidDomainEvent(Id, UserId, DateTime.UtcNow));
        UpdateTimestamp();
    }

    /// <summary>
    /// Reverts a reserved order back to Draft after a failed payment.
    /// Only allowed from Reserved status.
    /// </summary>
    /// <param name="reason">Failure reason reported by the payment provider.</param>
    /// <exception cref="DomainException">Thrown if the order is not in Reserved status.</exception>
    public void MarkAsPaymentFailed(string reason)
    {
        if (Status != OrderStatus.Reserved)
            throw new DomainException(ErrorCodes.OrderInvalidStatus, "Only reserved orders can be marked as payment failed.");

        Status = OrderStatus.Draft;
        ReservationExpiresAt = null;
        AddDomainEvent(new PaymentFailedDomainEvent(Id, UserId, reason, DateTime.UtcNow));
        UpdateTimestamp();
    }

    /// <summary>
    /// Cancels the order. Allowed from Draft or Reserved status.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the order is not in Draft or Reserved status.</exception>
    public void Cancel(string reason)
    {
        if (Status is not OrderStatus.Draft and not OrderStatus.Reserved)
            throw new DomainException(ErrorCodes.OrderInvalidStatus, $"Cannot cancel order with status '{Status}'.");

        Status = OrderStatus.Cancelled;
        ReservationExpiresAt = null;
        AddDomainEvent(new OrderCancelledDomainEvent(Id, UserId, reason, DateTime.UtcNow));
        UpdateTimestamp();
    }

    private void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
