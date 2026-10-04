namespace OrderService.Presentation;

/// <summary>
/// Named rate limiter policies used with <c>[EnableRateLimiting]</c>.
/// </summary>
internal static class RateLimitPolicies
{
    public const string Auth = "auth";
    public const string Payment = "payment";
    public const string Orders = "orders";
    public const string ProductsRead = "products-read";
}
