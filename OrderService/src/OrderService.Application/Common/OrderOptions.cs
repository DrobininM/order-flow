namespace OrderService.Application.Common;

/// <summary>
/// Configuration values needed by the Application layer for order processing.
/// Bound from the "Orders" section from settings.
/// </summary>
public sealed class OrderOptions
{
    public const string SectionName = "Orders";

    /// <summary>
    /// How many minutes a product reservation remains valid before it is released.
    /// </summary>
    public int ReservationTimeoutMinutes { get; init; } = 10;
}
