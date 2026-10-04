using ErrorOr;
using MediatR;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Products.Commands;
using OrderService.Application.Products.DTOs;
using OrderService.Domain.Entities;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Products.Handlers;

public sealed class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ErrorOr<ProductDto>>
{
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateProductCommandHandler(
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<ProductDto>> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        var moneyResult = Money.Create(request.Price, request.Currency);
        if (moneyResult.IsError)
            return moneyResult.Errors;

        var productResult = Product.Create(request.Name, request.Description, moneyResult.Value, request.StockQuantity);
        if (productResult.IsError)
            return productResult.Errors;

        var product = productResult.Value;

        await _productRepository.AddAsync(product, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return product.ToDto();
    }
}
