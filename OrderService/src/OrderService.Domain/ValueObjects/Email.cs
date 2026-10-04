using ErrorOr;
using OrderService.Domain.Common;

namespace OrderService.Domain.ValueObjects;

/// <summary>
/// Represents a validated email address as a value object.
/// </summary>
public sealed class Email : ValueObject
{
    /// <summary>
    /// Normalised email address (lowercase, trimmed).
    /// </summary>
    public string Value { get; }

    private Email(string value) => Value = value;

    /// <summary>
    /// Creates an Email after normalization.
    /// </summary>
    /// <param name="value">Raw email string.</param>
    /// <returns>A validated Email or an error.</returns>
    public static ErrorOr<Email> Create(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return Error.Validation(ErrorCodes.EmailEmpty, "Email cannot be empty.");

        var trimmed = value.Trim().ToLowerInvariant();

        if (!System.Net.Mail.MailAddress.TryCreate(trimmed, out _))
            return Error.Validation(ErrorCodes.EmailInvalid, $"'{trimmed}' is not a valid email address.");

        return new Email(trimmed);
    }

    public override string ToString() => Value;
    
    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }
}
