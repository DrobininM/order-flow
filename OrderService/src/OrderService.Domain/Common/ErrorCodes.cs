namespace OrderService.Domain.Common;

/// <summary>
/// Centralised error code constants used across Domain and Application layers.
/// </summary>
public static class ErrorCodes
{
    public const string ProductNotFound = "Product.NotFound";
    public const string ProductUnavailable = "Product.Unavailable";
    public const string ProductInsufficientStock = "Product.InsufficientStock";
    public const string ProductEmptyName = "Product.EmptyName";
    public const string ProductNegativeStock = "Product.NegativeStock";
    public const string ProductDeleteFailed = "Product.DeleteFailed";

    public const string OrderNotFound = "Order.NotFound";
    
    /// <summary>
    /// Order not owned by user.
    /// </summary>
    public const string OrderNotOwned = "Order.NotOwned";
    public const string OrderInvalidStatus = "Order.InvalidStatus";

    public const string AuthInvalid = "Auth.Invalid";
    public const string AuthInvalidRefresh = "Auth.InvalidRefresh";
    public const string UserExists = "User.Exists";
    public const string UserEmptyPassword = "User.EmptyPassword";
    public const string TokenEmpty = "Token.Empty";

    public const string MoneyNegative = "Money.Negative";
    public const string MoneyInvalidCurrency = "Money.InvalidCurrency";

    public const string EmailEmpty = "Email.Empty";
    public const string EmailInvalid = "Email.Invalid";

    public const string OrderItemEmptyName = "OrderItem.EmptyName";
    public const string OrderItemInvalidQuantity = "OrderItem.InvalidQuantity";
}
