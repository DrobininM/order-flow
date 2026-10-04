using ErrorOr;
using PaymentService.Domain.Common;
using PaymentService.Domain.DomainEvents;
using PaymentService.Domain.Enums;

namespace PaymentService.Domain.Entities;

/// <summary>
/// Represents a payment attempt for an order.
/// </summary>
public sealed class Payment : AggregateRoot<Guid>
{
    /// <summary>
    /// ID of the order being paid.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// ID of the user who owns the order.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// Amount to charge.
    /// </summary>
    public decimal Amount { get; private set; }

    /// <summary>
    /// Currency of the amount.
    /// </summary>
    public Currency Currency { get; private set; }

    /// <summary>
    /// Payment method selected by the user.
    /// </summary>
    public PaymentMethod Method { get; private set; }

    /// <summary>
    /// Current status of the payment.
    /// </summary>
    public PaymentStatus Status { get; private set; }

    /// <summary>
    /// Identifier returned by the external gateway once the payment succeeds.
    /// </summary>
    public string? ExternalPaymentId { get; private set; }

    /// <summary>
    /// Failure reason returned by the external gateway, if the payment failed.
    /// </summary>
    public string? ErrorMessage { get; private set; }

    /// <summary>
    /// Date and time when the payment was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Date and time when the payment reached a terminal state (succeeded or failed).
    /// </summary>
    public DateTime? CompletedAt { get; private set; }

    private Payment() { } // EF Core

    private Payment(
        Guid id,
        Guid orderId,
        Guid userId,
        decimal amount,
        Currency currency,
        PaymentMethod method) : base(id)
    {
        OrderId = orderId;
        UserId = userId;
        Amount = amount;
        Currency = currency;
        Method = method;
        Status = PaymentStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new pending payment.
    /// </summary>
    /// <param name="orderId">Order being paid. Must not be empty.</param>
    /// <param name="userId">Order owner. Must not be empty.</param>
    /// <param name="amount">Amount to charge. Must be positive.</param>
    /// <param name="currency">Currency of the amount.</param>
    /// <param name="method">Payment method.</param>
    public static ErrorOr<Payment> Create(
        Guid orderId,
        Guid userId,
        decimal amount,
        Currency currency,
        PaymentMethod method)
    {
        if (orderId == Guid.Empty)
            return Error.Validation(ErrorCodes.PaymentInvalidOrder, "Order id cannot be empty.");

        if (userId == Guid.Empty)
            return Error.Validation(ErrorCodes.PaymentInvalidUser, "User id cannot be empty.");

        if (amount <= 0)
            return Error.Validation(ErrorCodes.PaymentInvalidAmount, "Payment amount must be greater than zero.");

        var payment = new Payment(Guid.NewGuid(), orderId, userId, amount, currency, method);
        payment.AddDomainEvent(new PaymentCreatedDomainEvent(
            payment.Id, orderId, amount, currency.ToString(), DateTime.UtcNow));

        return payment;
    }

    /// <summary>
    /// Moves the payment to the <see cref="PaymentStatus.Processing"/> state.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the payment is not pending.</exception>
    public void MarkAsProcessing()
    {
        if (Status != PaymentStatus.Pending)
            throw new DomainException(ErrorCodes.PaymentInvalidStatus, "Only pending payments can be processed.");

        Status = PaymentStatus.Processing;
    }

    /// <summary>
    /// Marks the payment as succeeded and stores the external gateway identifier.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the payment is not processing or the id is empty.</exception>
    public void MarkAsSucceeded(string externalPaymentId)
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException(ErrorCodes.PaymentInvalidStatus, "Only processing payments can succeed.");

        if (string.IsNullOrWhiteSpace(externalPaymentId))
            throw new DomainException(ErrorCodes.PaymentExternalIdEmpty, "External payment id cannot be empty.");

        Status = PaymentStatus.Succeeded;
        ExternalPaymentId = externalPaymentId.Trim();
        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentSucceededDomainEvent(Id, OrderId, ExternalPaymentId, DateTime.UtcNow));
    }

    /// <summary>
    /// Marks the payment as failed and stores the gateway error.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the payment is not processing or the reason is empty.</exception>
    public void MarkAsFailed(string errorMessage)
    {
        if (Status != PaymentStatus.Processing)
            throw new DomainException(ErrorCodes.PaymentInvalidStatus, "Only processing payments can fail.");

        if (string.IsNullOrWhiteSpace(errorMessage))
            throw new DomainException(ErrorCodes.PaymentErrorEmpty, "Payment error message cannot be empty.");

        Status = PaymentStatus.Failed;
        ErrorMessage = errorMessage.Trim();
        CompletedAt = DateTime.UtcNow;

        AddDomainEvent(new PaymentFailedDomainEvent(Id, OrderId, ErrorMessage, DateTime.UtcNow));
    }

    /// <summary>
    /// Marks a succeeded payment as refunded.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the payment has not succeeded.</exception>
    public void MarkAsRefunded()
    {
        if (Status != PaymentStatus.Succeeded)
            throw new DomainException(ErrorCodes.PaymentInvalidStatus, "Only succeeded payments can be refunded.");

        Status = PaymentStatus.Refunded;

        AddDomainEvent(new PaymentRefundedDomainEvent(Id, OrderId, DateTime.UtcNow));
    }
}
