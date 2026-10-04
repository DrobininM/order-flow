using ErrorOr;
using PaymentService.Domain.Common;
using PaymentService.Domain.DomainEvents;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;

namespace PaymentService.Domain.UnitTests.Entities;

public sealed class PaymentTests
{
    private static readonly Guid OrderId = Guid.Parse("11111111-1111-1111-1111-111111111111");
    private static readonly Guid UserId = Guid.Parse("22222222-2222-2222-2222-222222222222");

    private static Payment CreatePayment(
        decimal amount = 100m,
        Currency currency = Currency.USD,
        PaymentMethod method = PaymentMethod.CreditCard)
    {
        var result = Payment.Create(OrderId, UserId, amount, currency, method);

        Assert.False(result.IsError);

        return result.Value;
    }

    [Fact]
    public void Create_ReturnsPendingPaymentWithAmountAndTimestamps()
    {
        var before = DateTime.UtcNow;

        var payment = CreatePayment(42.5m, Currency.EUR, PaymentMethod.Crypto);

        var after = DateTime.UtcNow;
        Assert.NotEqual(Guid.Empty, payment.Id);
        Assert.Equal(OrderId, payment.OrderId);
        Assert.Equal(UserId, payment.UserId);
        Assert.Equal(42.5m, payment.Amount);
        Assert.Equal(Currency.EUR, payment.Currency);
        Assert.Equal(PaymentMethod.Crypto, payment.Method);
        Assert.Equal(PaymentStatus.Pending, payment.Status);
        Assert.Null(payment.ExternalPaymentId);
        Assert.Null(payment.ErrorMessage);
        Assert.Null(payment.CompletedAt);
        Assert.InRange(payment.CreatedAt, before, after);
    }

    [Fact]
    public void Create_EachCallProducesUniqueId()
    {
        var first = CreatePayment();
        var second = CreatePayment();

        Assert.NotEqual(first.Id, second.Id);
    }

    [Fact]
    public void Create_RaisesPaymentCreatedDomainEvent()
    {
        var payment = CreatePayment(15m, Currency.RUB);

        var domainEvent = Assert.Single(payment.DequeueDomainEvents());
        var created = Assert.IsType<PaymentCreatedDomainEvent>(domainEvent);
        Assert.Equal(payment.Id, created.PaymentId);
        Assert.Equal(OrderId, created.OrderId);
        Assert.Equal(15m, created.Amount);
        Assert.Equal(Currency.RUB.ToString(), created.Currency);
    }

    [Fact]
    public void Create_WithEmptyOrderId_ReturnsValidationError()
    {
        var result = Payment.Create(Guid.Empty, UserId, 10m, Currency.USD, PaymentMethod.CreditCard);

        Assert.True(result.IsError);
        Assert.Equal(ErrorCodes.PaymentInvalidOrder, result.FirstError.Code);
        Assert.Equal(ErrorType.Validation, result.FirstError.Type);
    }

    [Fact]
    public void Create_WithEmptyUserId_ReturnsValidationError()
    {
        var result = Payment.Create(OrderId, Guid.Empty, 10m, Currency.USD, PaymentMethod.CreditCard);

        Assert.True(result.IsError);
        Assert.Equal(ErrorCodes.PaymentInvalidUser, result.FirstError.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-99.99)]
    public void Create_WithNonPositiveAmount_ReturnsValidationError(decimal amount)
    {
        var result = Payment.Create(OrderId, UserId, amount, Currency.USD, PaymentMethod.CreditCard);

        Assert.True(result.IsError);
        Assert.Equal(ErrorCodes.PaymentInvalidAmount, result.FirstError.Code);
    }

    [Fact]
    public void MarkAsProcessing_FromPending_SetsProcessing()
    {
        var payment = CreatePayment();

        payment.MarkAsProcessing();

        Assert.Equal(PaymentStatus.Processing, payment.Status);
        Assert.Null(payment.CompletedAt);
    }

    [Fact]
    public void MarkAsProcessing_WhenAlreadyProcessing_ThrowsDomainException()
    {
        var payment = CreatePayment();
        payment.MarkAsProcessing();

        var exception = Assert.Throws<DomainException>(payment.MarkAsProcessing);

        Assert.Equal(ErrorCodes.PaymentInvalidStatus, exception.Code);
    }

    [Fact]
    public void MarkAsSucceeded_FromProcessing_SetsStatusExternalIdAndCompletedAt()
    {
        var payment = CreatePayment();
        payment.MarkAsProcessing();
        var before = DateTime.UtcNow;

        payment.MarkAsSucceeded("ext-123");

        var after = DateTime.UtcNow;
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal("ext-123", payment.ExternalPaymentId);
        Assert.NotNull(payment.CompletedAt);
        Assert.InRange(payment.CompletedAt!.Value, before, after);
    }

    [Fact]
    public void MarkAsSucceeded_RaisesPaymentSucceededDomainEvent()
    {
        var payment = CreatePayment();
        payment.MarkAsProcessing();
        payment.DequeueDomainEvents();

        payment.MarkAsSucceeded("ext-123");

        var succeeded = Assert.IsType<PaymentSucceededDomainEvent>(
            Assert.Single(payment.DequeueDomainEvents()));
        Assert.Equal(payment.Id, succeeded.PaymentId);
        Assert.Equal(OrderId, succeeded.OrderId);
        Assert.Equal("ext-123", succeeded.ExternalPaymentId);
    }

    [Fact]
    public void MarkAsSucceeded_WhenNotProcessing_ThrowsDomainException()
    {
        var payment = CreatePayment();

        var exception = Assert.Throws<DomainException>(() => payment.MarkAsSucceeded("ext-123"));

        Assert.Equal(ErrorCodes.PaymentInvalidStatus, exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkAsSucceeded_WithEmptyExternalId_ThrowsDomainException(string externalPaymentId)
    {
        var payment = CreatePayment();
        payment.MarkAsProcessing();

        var exception = Assert.Throws<DomainException>(() => payment.MarkAsSucceeded(externalPaymentId));

        Assert.Equal(ErrorCodes.PaymentExternalIdEmpty, exception.Code);
    }

    [Fact]
    public void MarkAsFailed_FromProcessing_SetsStatusErrorAndCompletedAt()
    {
        var payment = CreatePayment();
        payment.MarkAsProcessing();
        var before = DateTime.UtcNow;

        payment.MarkAsFailed("insufficient funds");

        var after = DateTime.UtcNow;
        Assert.Equal(PaymentStatus.Failed, payment.Status);
        Assert.Equal("insufficient funds", payment.ErrorMessage);
        Assert.NotNull(payment.CompletedAt);
        Assert.InRange(payment.CompletedAt!.Value, before, after);
    }

    [Fact]
    public void MarkAsFailed_RaisesPaymentFailedDomainEventWithReason()
    {
        var payment = CreatePayment();
        payment.MarkAsProcessing();
        payment.DequeueDomainEvents();

        payment.MarkAsFailed("declined");

        var failed = Assert.IsType<PaymentFailedDomainEvent>(
            Assert.Single(payment.DequeueDomainEvents()));
        Assert.Equal(payment.Id, failed.PaymentId);
        Assert.Equal(OrderId, failed.OrderId);
        Assert.Equal("declined", failed.Error);
    }

    [Fact]
    public void MarkAsFailed_WhenNotProcessing_ThrowsDomainException()
    {
        var payment = CreatePayment();

        var exception = Assert.Throws<DomainException>(() => payment.MarkAsFailed("declined"));

        Assert.Equal(ErrorCodes.PaymentInvalidStatus, exception.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void MarkAsFailed_WithEmptyReason_ThrowsDomainException(string reason)
    {
        var payment = CreatePayment();
        payment.MarkAsProcessing();

        var exception = Assert.Throws<DomainException>(() => payment.MarkAsFailed(reason));

        Assert.Equal(ErrorCodes.PaymentErrorEmpty, exception.Code);
    }

    [Fact]
    public void MarkAsRefunded_FromSucceeded_SetsRefunded()
    {
        var payment = CreatePayment();
        payment.MarkAsProcessing();
        payment.MarkAsSucceeded("ext-123");

        payment.MarkAsRefunded();

        Assert.Equal(PaymentStatus.Refunded, payment.Status);
    }

    [Fact]
    public void MarkAsRefunded_RaisesPaymentRefundedDomainEvent()
    {
        var payment = CreatePayment();
        payment.MarkAsProcessing();
        payment.MarkAsSucceeded("ext-123");
        payment.DequeueDomainEvents();

        payment.MarkAsRefunded();

        var refunded = Assert.IsType<PaymentRefundedDomainEvent>(
            Assert.Single(payment.DequeueDomainEvents()));
        Assert.Equal(payment.Id, refunded.PaymentId);
        Assert.Equal(OrderId, refunded.OrderId);
    }

    [Theory]
    [InlineData(PaymentStatus.Pending)]
    [InlineData(PaymentStatus.Processing)]
    [InlineData(PaymentStatus.Failed)]
    public void MarkAsRefunded_WhenNotSucceeded_ThrowsDomainException(PaymentStatus status)
    {
        var payment = CreatePayment();

        if (status == PaymentStatus.Processing)
        {
            payment.MarkAsProcessing();
        }
        else if (status == PaymentStatus.Failed)
        {
            payment.MarkAsProcessing();
            payment.MarkAsFailed("declined");
        }

        var exception = Assert.Throws<DomainException>(payment.MarkAsRefunded);

        Assert.Equal(ErrorCodes.PaymentInvalidStatus, exception.Code);
    }

    [Fact]
    public void DequeueDomainEvents_ReturnsEventsAndClearsThem()
    {
        var payment = CreatePayment();

        Assert.Single(payment.DequeueDomainEvents());
        Assert.Empty(payment.DequeueDomainEvents());
    }
}
