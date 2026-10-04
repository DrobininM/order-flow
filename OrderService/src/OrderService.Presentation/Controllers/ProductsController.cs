using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderService.Application.Products.Commands;
using OrderService.Application.Products.DTOs;
using OrderService.Application.Products.Queries;
using OrderService.Presentation;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// Manages the product catalog. Admin-only for create, update, and delete.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class ProductsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Creates a new instance of <see cref="ProductsController"/>.
    /// </summary>
    public ProductsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Returns a paged list of available products.
    /// </summary>
    [HttpGet]
    [EnableRateLimiting(RateLimitPolicies.ProductsRead)]
    [ProducesResponseType(typeof(PagedProductsDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetProducts([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var result = await _sender.Send(new GetProductsQuery(skip, take));

        return result.Match<IActionResult>(
            Ok,
            errors => Problem(statusCode: 500, detail: errors[0].Description));
    }

    /// <summary>
    /// Returns a single product by ID.
    /// </summary>
    [HttpGet("{id:guid}")]
    [EnableRateLimiting(RateLimitPolicies.ProductsRead)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetProduct(Guid id)
    {
        var result = await _sender.Send(new GetProductQuery(id));

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Creates a new product. Admin only.
    /// </summary>
    [HttpPost]
    [Authorize(Roles = RoleConstants.Admin)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateProduct([FromBody] CreateProductCommand command)
    {
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            value => CreatedAtAction(nameof(GetProduct), new { id = value.Id }, value),
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Updates an existing product. Admin only.
    /// </summary>
    [HttpPut("{id:guid}")]
    [Authorize(Roles = RoleConstants.Admin)]
    [ProducesResponseType(typeof(ProductDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateProduct(Guid id, [FromBody] UpdateProductCommand command)
    {
        if (id != command.Id)
            return BadRequest("Id mismatch.");

        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Soft-deletes a product (marks as unavailable). Admin only.
    /// </summary>
    [HttpDelete("{id:guid}")]
    [Authorize(Roles = RoleConstants.Admin)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var result = await _sender.Send(new DeleteProductCommand(id));

        return result.Match<IActionResult>(
            _ => Ok(new { Message = "Product deleted." }),
            errors => ControllerErrorHelper.MapError(errors[0]));
    }
}
