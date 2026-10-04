namespace PaymentService.Domain.Common;

/// <summary>
/// Base exception for domain-level rule violations.
/// Carries an error code and a human-readable message.
/// </summary>
public class DomainException : Exception
{
    /// <summary>
    /// Error code identifying the specific rule violation (e.g. "Payment.InvalidStatus").
    /// </summary>
    public string Code { get; }

    /// <summary>
    /// Creates a new domain exception.
    /// </summary>
    /// <param name="code">Error code from <see cref="ErrorCodes"/>.</param>
    /// <param name="message">Human-readable description.</param>
    public DomainException(string code, string message)
        : base(message)
    {
        Code = code;
    }

    /// <summary>
    /// Creates a new domain exception with an inner exception.
    /// </summary>
    public DomainException(string code, string message, Exception inner)
        : base(message, inner)
    {
        Code = code;
    }
}
