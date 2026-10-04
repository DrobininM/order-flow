using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Products.Commands;
using OrderService.Domain.Common;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Products.Handlers;

public sealed class DeleteProductCommandHandler : IRequestHandler<DeleteProductCommand, ErrorOr<Deleted>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICacheService _cacheService;
    private readonly ILogger<DeleteProductCommandHandler> _logger;

    public DeleteProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork,
        ICacheService cacheService,
        ILogger<DeleteProductCommandHandler> logger)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
        _cacheService = cacheService;
        _logger = logger;
    }

    public async Task<ErrorOr<Deleted>> Handle(DeleteProductCommand request, CancellationToken cancellationToken)
    {
        var product = await _productRepository.GetByIdAsync(request.Id, cancellationToken);
        if (product is null)
            return Error.NotFound(ErrorCodes.ProductNotFound, $"Product with id '{request.Id}' not found.");

        try
        {
            product.MarkUnavailable();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to mark product {ProductId} as unavailable.", request.Id);
            
            return Error.Unexpected(ErrorCodes.ProductDeleteFailed, "Failed to delete product.");
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        await _cacheService.RemoveAsync($"product:{request.Id}", cancellationToken);

        return Result.Deleted;
    }
}
