using FluentValidation;

namespace OrderService.Application.Products.Commands;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        Include(new ProductFieldsValidator());
    }
}

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator()
    {
        RuleFor(x => x.Id).NotEmpty();
        Include(new ProductFieldsValidator());
    }
}

/// <summary>
/// Shared validation rules for product name, description, price, currency, and stock quantity.
/// </summary>
internal sealed class ProductFieldsValidator : AbstractValidator<IProductCommand>
{
    public ProductFieldsValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Description).MaximumLength(2000);
        RuleFor(x => x.Price).GreaterThan(0);
        RuleFor(x => x.Currency).NotEmpty().Length(3);
        RuleFor(x => x.StockQuantity).GreaterThanOrEqualTo(0);
    }
}

/// <summary>
/// Common interface for commands that contain product field data.
/// </summary>
public interface IProductCommand
{
    string Name { get; }
    string Description { get; }
    decimal Price { get; }
    string Currency { get; }
    int StockQuantity { get; }
}
