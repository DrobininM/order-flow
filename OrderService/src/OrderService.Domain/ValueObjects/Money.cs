using ErrorOr;
using OrderService.Domain.Common;
using OrderService.Domain.Enums;

namespace OrderService.Domain.ValueObjects;

/// <summary>
/// Represents a monetary value with a specific currency.
/// </summary>
public sealed class Money : ValueObject
{
    /// <summary>
    /// Numeric amount.
    /// </summary>
    public decimal Amount { get; }

    /// <summary>
    /// Currency of the amount.
    /// </summary>
    public Currency Currency { get; }

    internal Money(decimal amount, Currency currency)
    {
        Amount = amount;
        Currency = currency;
    }

    /// <summary>
    /// Creates a Money instance with validation.
    /// </summary>
    /// <param name="amount">Must not be negative.</param>
    /// <param name="currency">The currency.</param>
    public static ErrorOr<Money> Create(decimal amount, Currency currency)
    {
        if (amount < 0)
            return Error.Validation(ErrorCodes.MoneyNegative, "Amount cannot be negative.");

        return new Money(amount, currency);
    }

    /// <summary>
    /// Creates a Money instance from a string currency code.
    /// </summary>
    public static ErrorOr<Money> Create(decimal amount, string currency)
    {
        if (!Enum.TryParse<Currency>(currency, true, out var parsed))
            return Error.Validation(ErrorCodes.MoneyInvalidCurrency, $"Unknown currency: '{currency}'.");

        return Create(amount, parsed);
    }

    /// <summary>
    /// Returns a zero-amount Money for the given currency.
    /// </summary>
    public static Money Zero(Currency currency) => new(0m, currency);

    /// <summary>
    /// Adds two Money values of the same currency.
    /// </summary>
    /// <exception cref="DomainException">Thrown if currencies differ.</exception>
    public Money Add(Money other)
    {
        if (Currency != other.Currency)
            throw new DomainException(ErrorCodes.MoneyInvalidCurrency, "Cannot add money with different currencies.");

        return new Money(Amount + other.Amount, Currency);
    }

    /// <summary>
    /// Multiplies the amount by a positive integer multiplier.
    /// </summary>
    /// <exception cref="DomainException">Thrown if multiplier is zero or negative.</exception>
    public Money Multiply(int multiplier)
    {
        if (multiplier <= 0)
            throw new DomainException(ErrorCodes.MoneyNegative, "Multiplier must be positive.");

        return new Money(Amount * multiplier, Currency);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Amount;
        yield return Currency;
    }

    public override string ToString() => $"{Amount:F2} {Currency}";
}
