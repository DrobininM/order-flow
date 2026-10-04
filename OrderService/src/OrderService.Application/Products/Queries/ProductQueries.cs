using ErrorOr;
using MediatR;
using OrderService.Application.Products.DTOs;

namespace OrderService.Application.Products.Queries;

public sealed record GetProductQuery(Guid Id) : IRequest<ErrorOr<ProductDto>>;

public sealed record GetProductsQuery(int Skip = 0, int Take = 20) : IRequest<ErrorOr<PagedProductsDto>>;

public sealed record PagedProductsDto(IReadOnlyList<ProductDto> Items, int TotalCount, int Skip, int Take);
