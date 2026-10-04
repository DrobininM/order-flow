using ErrorOr;
using OrderService.Domain.Common;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

/// <summary>
/// Represents a product in the catalog.
/// </summary>
public sealed class Product : AggregateRoot<Guid>
{
    /// <summary>
    /// Actual product name.
    /// </summary>
    public string Name { get; private set; } = null!;

    /// <summary>
    /// Product description.
    /// </summary>
    public string Description { get; private set; } = null!;

    /// <summary>
    /// Product price.
    /// </summary>
    public Money Price { get; private set; } = null!;

    /// <summary>
    /// Total stock quantity.
    /// </summary>
    public int StockQuantity { get; private set; }

    /// <summary>
    /// Quantity currently reserved by pending orders.
    /// </summary>
    public int ReservedQuantity { get; private set; }

    /// <summary>
    /// Whether the product is available for purchase.
    /// </summary>
    public bool IsAvailable { get; private set; }

    /// <summary>
    /// Date and time when the product was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// When the product was last updated.
    /// </summary>
    public DateTime UpdatedAt { get; private set; }

    /// <summary>
    /// Available quantity that can be reserved (stock minus reserved).
    /// </summary>
    public int AvailableQuantity => StockQuantity - ReservedQuantity;

    /// <summary>
    /// Optimistic concurrency version.
    /// </summary>
    public uint Version { get; private set; }

    private Product() { } // EF Core

    private Product(Guid id, string name, string description, Money price, int stockQuantity) : base(id)
    {
        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        ReservedQuantity = 0;
        IsAvailable = true;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new product.
    /// </summary>
    /// <param name="name">Product name. Must not be empty.</param>
    /// <param name="description">Product description.</param>
    /// <param name="price">Product price. Must be non-negative.</param>
    /// <param name="stockQuantity">Initial stock quantity. Must not be negative.</param>
    public static ErrorOr<Product> Create(string name, string description, Money price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Error.Validation(ErrorCodes.ProductEmptyName, "Product name cannot be empty.");

        if (stockQuantity < 0)
            return Error.Validation(ErrorCodes.ProductNegativeStock, "Stock quantity cannot be negative.");

        return new Product(Guid.NewGuid(), name, description, price, stockQuantity);
    }

    /// <summary>
    /// Checks whether the given quantity can be reserved.
    /// </summary>
    public bool CanReserve(int quantity) =>
        IsAvailable && AvailableQuantity >= quantity && quantity > 0;

    /// <summary>
    /// Reserves the specified quantity.
    /// </summary>
    /// <exception cref="DomainException">Thrown if the quantity cannot be reserved.</exception>
    public void Reserve(int quantity)
    {
        if (!CanReserve(quantity))
            throw new DomainException(ErrorCodes.ProductInsufficientStock,
                $"Cannot reserve {quantity} units. Available: {AvailableQuantity}.");

        ReservedQuantity += quantity;
        
        UpdateTimestamp();
    }

    /// <summary>
    /// Releases a previously reserved quantity.
    /// </summary>
    /// <exception cref="DomainException">Thrown if releasing more than reserved.</exception>
    public void ReleaseReservation(int quantity)
    {
        if (quantity > ReservedQuantity)
            throw new DomainException(ErrorCodes.ProductInsufficientStock,
                $"Cannot release {quantity} reserved units. Reserved: {ReservedQuantity}.");

        ReservedQuantity -= quantity;
        
        UpdateTimestamp();
    }

    /// <summary>
    /// Commits a reservation, reducing both stock and reserved counts.
    /// </summary>
    /// <exception cref="DomainException">Thrown if committing more than reserved.</exception>
    public void CommitReservation(int quantity)
    {
        if (quantity > ReservedQuantity)
            throw new DomainException(ErrorCodes.ProductInsufficientStock,
                $"Cannot commit {quantity} reserved units. Reserved: {ReservedQuantity}.");

        StockQuantity -= quantity;
        ReservedQuantity -= quantity;
        
        UpdateTimestamp();
    }

    /// <summary>
    /// Updates product details.
    /// </summary>
    public void Update(string name, string description, Money price, int stockQuantity)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException(ErrorCodes.ProductEmptyName, "Product name cannot be empty.");

        if (stockQuantity < 0)
            throw new DomainException(ErrorCodes.ProductNegativeStock, "Stock quantity cannot be negative.");

        Name = name;
        Description = description;
        Price = price;
        StockQuantity = stockQuantity;
        
        UpdateTimestamp();
    }

    /// <summary>
    /// Marks the product as unavailable.
    /// </summary>
    public void MarkUnavailable()
    {
        IsAvailable = false;
        UpdatedAt = DateTime.UtcNow;
    }
    
    private void UpdateTimestamp()
    {
        UpdatedAt = DateTime.UtcNow;
    }
}
