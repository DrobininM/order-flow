namespace OrderService.Application.Products.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity,
    int AvailableQuantity,
    bool IsAvailable,
    DateTime CreatedAt);
