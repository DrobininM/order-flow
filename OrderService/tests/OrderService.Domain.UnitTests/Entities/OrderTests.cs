using OrderService.Domain.Common;
using OrderService.Domain.DomainEvents;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.UnitTests.Entities;

public sealed class OrderTests
{
    private static readonly Guid UserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid ProductId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static Money Price(decimal amount, Currency currency = Currency.USD) =>
        Money.Create(amount, currency).Value;

    [Fact]
    public void Create_ReturnsDraftOrderWithUserAndTimestamps()
    {
        var before = DateTime.UtcNow;

        var order = Order.Create(UserId);

        var after = DateTime.UtcNow;
        Assert.NotEqual(Guid.Empty, order.Id);
        Assert.Equal(UserId, order.UserId);
        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Empty(order.Items);
        Assert.Null(order.ReservationExpiresAt);
        Assert.InRange(order.CreatedAt, before, after);
        Assert.InRange(order.UpdatedAt, before, after);
        Assert.Equal(0u, order.Version);
    }

    [Fact]
    public void Create_RaisesOrderCreatedDomainEvent()
    {
        var order = Order.Create(UserId);

        var domainEvent = Assert.Single(order.DequeueDomainEvents());
        var created = Assert.IsType<OrderCreatedDomainEvent>(domainEvent);
        Assert.Equal(order.Id, created.OrderId);
        Assert.Equal(UserId, created.UserId);
        Assert.Equal(order.CreatedAt, created.OccurredOn);
    }

    [Fact]
    public void Create_EachCallProducesUniqueId()
    {
        var first = Order.Create(UserId);
        var second = Order.Create(UserId);

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void AddItem_AddsItemWithProductSnapshot()
    {
        var order = Order.Create(UserId);

        var item = order.AddItem(ProductId, "Widget", Price(10m), 3);

        Assert.Same(item, Assert.Single(order.Items));
        Assert.Equal(order.Id, item.OrderId);
        Assert.Equal(ProductId, item.ProductId);
        Assert.Equal("Widget", item.ProductName);
        Assert.Equal(10m, item.UnitPrice);
        Assert.Equal(Currency.USD, item.Currency);
        Assert.Equal(3, item.Quantity);
        Assert.Equal(30m, item.TotalPrice);
    }

    [Fact]
    public void AddItem_SameProduct_AccumulatesQuantityOnExistingItem()
    {
        var order = Order.Create(UserId);

        var first = order.AddItem(ProductId, "Widget", Price(10m), 2);
        var second = order.AddItem(ProductId, "Widget", Price(10m), 5);

        Assert.Same(first, second);
        Assert.Single(order.Items);
        Assert.Equal(7, first.Quantity);
    }

    [Fact]
    public void AddItem_UpdatesTimestamp()
    {
        var order = Order.Create(UserId);
        var createdAt = order.CreatedAt;

        Thread.Sleep(5);
        order.AddItem(ProductId, "Widget", Price(10m), 1);

        Assert.True(order.UpdatedAt > createdAt);
    }

    [Fact]
    public void AddItem_WhenNotDraft_ThrowsDomainException()
    {
        var order = Order.Create(UserId);
        order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));

        var exception = Assert.Throws<DomainException>(
            () => order.AddItem(ProductId, "Widget", Price(10m), 1));

        Assert.Equal(ErrorCodes.OrderInvalidStatus, exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void AddItem_WithNonPositiveQuantity_ThrowsDomainException(int quantity)
    {
        var order = Order.Create(UserId);

        var exception = Assert.Throws<DomainException>(
            () => order.AddItem(ProductId, "Widget", Price(10m), quantity));

        Assert.Equal(ErrorCodes.OrderItemInvalidQuantity, exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void AddItem_WithEmptyProductName_ThrowsDomainException(string productName)
    {
        var order = Order.Create(UserId);

        var exception = Assert.Throws<DomainException>(
            () => order.AddItem(ProductId, productName, Price(10m), 1));

        Assert.Equal(ErrorCodes.OrderItemEmptyName, exception.Code);
    }

    [Fact]
    public void RemoveItem_RemovesMatchingItem()
    {
        var order = Order.Create(UserId);
        order.AddItem(ProductId, "Widget", Price(10m), 1);

        order.RemoveItem(ProductId);

        Assert.Empty(order.Items);
    }

    [Fact]
    public void RemoveItem_WithUnknownProduct_IsNoOp()
    {
        var order = Order.Create(UserId);
        order.AddItem(ProductId, "Widget", Price(10m), 1);

        order.RemoveItem(Guid.NewGuid());

        Assert.Single(order.Items);
    }

    [Fact]
    public void RemoveItem_WhenNotDraft_ThrowsDomainException()
    {
        var order = Order.Create(UserId);
        order.AddItem(ProductId, "Widget", Price(10m), 1);
        order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));

        var exception = Assert.Throws<DomainException>(() => order.RemoveItem(ProductId));

        Assert.Equal(ErrorCodes.OrderInvalidStatus, exception.Code);
    }

    [Fact]
    public void GetTotalAmount_WhenEmpty_ReturnsZeroUsd()
    {
        var order = Order.Create(UserId);

        var total = order.GetTotalAmount();

        Assert.Equal(0m, total.Amount);
        Assert.Equal(Currency.USD, total.Currency);
    }

    [Fact]
    public void GetTotalAmount_SumsAllItems()
    {
        var order = Order.Create(UserId);
        order.AddItem(ProductId, "Widget", Price(10m), 2);
        order.AddItem(Guid.NewGuid(), "Gadget", Price(5.5m), 4);

        var total = order.GetTotalAmount();

        Assert.Equal(42m, total.Amount);
        Assert.Equal(Currency.USD, total.Currency);
    }

    [Fact]
    public void MarkAsReserved_FromDraft_SetsStatusAndExpiration()
    {
        var order = Order.Create(UserId);
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        order.MarkAsReserved(expiresAt);

        Assert.Equal(OrderStatus.Reserved, order.Status);
        Assert.Equal(expiresAt, order.ReservationExpiresAt);
    }

    [Fact]
    public void MarkAsReserved_RaisesOrderReservedDomainEvent()
    {
        var order = Order.Create(UserId);
        order.DequeueDomainEvents();

        order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));

        var reserved = Assert.IsType<OrderReservedDomainEvent>(
            Assert.Single(order.DequeueDomainEvents()));
        Assert.Equal(order.Id, reserved.OrderId);
        Assert.Equal(UserId, reserved.UserId);
    }

    [Fact]
    public void MarkAsReserved_WhenNotDraft_ThrowsDomainException()
    {
        var order = Order.Create(UserId);
        order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));

        var exception = Assert.Throws<DomainException>(
            () => order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15)));

        Assert.Equal(ErrorCodes.OrderInvalidStatus, exception.Code);
    }

    [Fact]
    public void MarkAsPaid_FromReserved_SetsPaidAndClearsExpiration()
    {
        var order = Order.Create(UserId);
        order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));

        order.MarkAsPaid();

        Assert.Equal(OrderStatus.Paid, order.Status);
        Assert.Null(order.ReservationExpiresAt);
    }

    [Fact]
    public void MarkAsPaid_RaisesOrderPaidDomainEvent()
    {
        var order = Order.Create(UserId);
        order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));
        order.DequeueDomainEvents();

        order.MarkAsPaid();

        var paid = Assert.IsType<OrderPaidDomainEvent>(Assert.Single(order.DequeueDomainEvents()));
        Assert.Equal(order.Id, paid.OrderId);
        Assert.Equal(UserId, paid.UserId);
    }

    [Fact]
    public void MarkAsPaid_WhenNotReserved_ThrowsDomainException()
    {
        var order = Order.Create(UserId);

        var exception = Assert.Throws<DomainException>(() => order.MarkAsPaid());

        Assert.Equal(ErrorCodes.OrderInvalidStatus, exception.Code);
    }

    [Fact]
    public void MarkAsPaymentFailed_FromReserved_RevertsToDraft()
    {
        var order = Order.Create(UserId);
        order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));

        order.MarkAsPaymentFailed("declined");

        Assert.Equal(OrderStatus.Draft, order.Status);
        Assert.Null(order.ReservationExpiresAt);
    }

    [Fact]
    public void MarkAsPaymentFailed_RaisesEventWithReason()
    {
        var order = Order.Create(UserId);
        order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));
        order.DequeueDomainEvents();

        order.MarkAsPaymentFailed("insufficient funds");

        var failed = Assert.IsType<PaymentFailedDomainEvent>(
            Assert.Single(order.DequeueDomainEvents()));
        Assert.Equal(order.Id, failed.OrderId);
        Assert.Equal(UserId, failed.UserId);
        Assert.Equal("insufficient funds", failed.Error);
    }

    [Fact]
    public void MarkAsPaymentFailed_WhenNotReserved_ThrowsDomainException()
    {
        var order = Order.Create(UserId);

        var exception = Assert.Throws<DomainException>(() => order.MarkAsPaymentFailed("declined"));

        Assert.Equal(ErrorCodes.OrderInvalidStatus, exception.Code);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Cancel_FromDraftOrReserved_SetsCancelledAndClearsExpiration(bool reserve)
    {
        var order = Order.Create(UserId);
        if (reserve)
        {
            order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));
        }

        order.Cancel("customer request");

        Assert.Equal(OrderStatus.Cancelled, order.Status);
        Assert.Null(order.ReservationExpiresAt);
    }

    [Fact]
    public void Cancel_RaisesOrderCancelledEventWithReason()
    {
        var order = Order.Create(UserId);
        order.DequeueDomainEvents();

        order.Cancel("changed my mind");

        var cancelled = Assert.IsType<OrderCancelledDomainEvent>(
            Assert.Single(order.DequeueDomainEvents()));
        Assert.Equal(order.Id, cancelled.OrderId);
        Assert.Equal(UserId, cancelled.UserId);
        Assert.Equal("changed my mind", cancelled.Reason);
    }

    [Fact]
    public void Cancel_WhenPaid_ThrowsDomainException()
    {
        var order = Order.Create(UserId);
        order.MarkAsReserved(DateTime.UtcNow.AddMinutes(15));
        order.MarkAsPaid();

        var exception = Assert.Throws<DomainException>(() => order.Cancel("too late"));

        Assert.Equal(ErrorCodes.OrderInvalidStatus, exception.Code);
    }

    [Fact]
    public void Cancel_WhenAlreadyCancelled_ThrowsDomainException()
    {
        var order = Order.Create(UserId);
        order.Cancel("first");

        var exception = Assert.Throws<DomainException>(() => order.Cancel("second"));

        Assert.Equal(ErrorCodes.OrderInvalidStatus, exception.Code);
    }

    [Fact]
    public void DequeueDomainEvents_ReturnsEventsAndClearsThem()
    {
        var order = Order.Create(UserId);

        Assert.Single(order.DequeueDomainEvents());
        Assert.Empty(order.DequeueDomainEvents());
    }
}
