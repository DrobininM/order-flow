using ErrorOr;
using MediatR;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Products.DTOs;
using OrderService.Application.Products.Queries;
using OrderService.Domain.Common;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Products.Handlers;

public sealed class GetProductQueryHandler : IRequestHandler<GetProductQuery, ErrorOr<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;

    public GetProductQueryHandler(IProductRepository productRepository, ICacheService cacheService)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
    }

    public async Task<ErrorOr<ProductDto>> Handle(GetProductQuery request, CancellationToken cancellationToken)
    {
        var cacheKey = $"product:{request.Id}";

        var cached = await _cacheService.GetAsync<ProductDto>(cacheKey, cancellationToken);

        if (cached is not null)
            return cached;

        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);

        if (product is null)
            return Error.NotFound(ErrorCodes.ProductNotFound, $"Product with id '{request.Id}' not found.");

        var dto = product.ToDto();

        await _cacheService.SetAsync(cacheKey, dto, TimeSpan.FromMinutes(5), cancellationToken);

        return dto;
    }
}
