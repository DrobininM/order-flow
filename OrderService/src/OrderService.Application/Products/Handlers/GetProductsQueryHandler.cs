using ErrorOr;
using MediatR;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Products.Queries;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Products.Handlers;

public sealed class GetProductsQueryHandler : IRequestHandler<GetProductsQuery, ErrorOr<PagedProductsDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly ICacheService _cacheService;

    public GetProductsQueryHandler(IProductRepository productRepository, ICacheService cacheService)
    {
        _productRepository = productRepository;
        _cacheService = cacheService;
    }

    public async Task<ErrorOr<PagedProductsDto>> Handle(GetProductsQuery request, CancellationToken cancellationToken)
    {
        var products = await _productRepository.GetAvailableAsync(request.Skip, request.Take, cancellationToken);
        var totalCount = await _productRepository.GetTotalCountAsync(cancellationToken);

        var items = products.Select(p => p.ToDto()).ToList();

        return new PagedProductsDto(items, totalCount, request.Skip, request.Take);
    }
}
