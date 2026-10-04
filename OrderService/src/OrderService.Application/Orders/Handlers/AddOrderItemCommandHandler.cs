using ErrorOr;
using MediatR;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Application.Orders.Commands;
using OrderService.Application.Orders.DTOs;
using OrderService.Domain.Common;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Orders.Handlers;

public sealed class AddOrderItemCommandHandler : IRequestHandler<AddOrderItemCommand, ErrorOr<OrderDto>>
{
    private readonly IOrderRepository _orderRepository;
    private readonly IProductRepository _productRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddOrderItemCommandHandler(
        IOrderRepository orderRepository,
        IProductRepository productRepository,
        IUnitOfWork unitOfWork)
    {
        _orderRepository = orderRepository;
        _productRepository = productRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<ErrorOr<OrderDto>> Handle(AddOrderItemCommand request, CancellationToken cancellationToken)
    {
        var order = await _orderRepository.GetByIdWithItemsAsync(request.OrderId, cancellationToken);

        if (order is null)
            return Error.NotFound(ErrorCodes.OrderNotFound, $"Order {request.OrderId} not found.");

        var product = await _productRepository.GetByIdAsync(request.ProductId, cancellationToken);

        if (product is null)
            return Error.NotFound(ErrorCodes.ProductNotFound, $"Product {request.ProductId} not found.");

        if (!product.IsAvailable || product.AvailableQuantity < request.Quantity)
            return Error.Validation(ErrorCodes.ProductInsufficientStock, $"Insufficient stock for '{product.Name}'.");

        try
        {
            order.AddItem(product.Id, product.Name, product.Price, request.Quantity);
        }
        catch (DomainException ex)
        {
            return Error.Validation(ex.Code, ex.Message);
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return order.ToDto();
    }
}
