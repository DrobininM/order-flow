namespace PaymentService.Infrastructure.Auth;

/// <summary>
/// JWT settings used to validate access tokens issued by OrderService.
/// Bound from the "Jwt" configuration section.
/// </summary>
public sealed class JwtOptions
{
    public const string SectionName = "Jwt";

    /// <summary>
    /// HMAC-SHA256 signing key. Must match the one used by the issuing service.
    /// </summary>
    public string SecretKey { get; init; } = string.Empty;

    /// <summary>
    /// Expected token issuer.
    /// </summary>
    public string Issuer { get; init; } = "OrderService";

    /// <summary>
    /// Expected token audience.
    /// </summary>
    public string Audience { get; init; } = "OrderService";
}
