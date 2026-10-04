using ErrorOr;
using MediatR;
using OrderService.Application.Products.DTOs;

namespace OrderService.Application.Products.Commands;

public sealed record CreateProductCommand(
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity) : IRequest<ErrorOr<ProductDto>>, IProductCommand;

public sealed record UpdateProductCommand(
    Guid Id,
    string Name,
    string Description,
    decimal Price,
    string Currency,
    int StockQuantity) : IRequest<ErrorOr<ProductDto>>, IProductCommand;

public sealed record DeleteProductCommand(Guid Id) : IRequest<ErrorOr<Deleted>>;
