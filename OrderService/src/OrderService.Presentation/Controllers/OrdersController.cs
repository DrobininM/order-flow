using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderService.Application.Orders.Commands;
using OrderService.Application.Orders.DTOs;
using OrderService.Application.Orders.Queries;
using OrderService.Presentation;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// Manages customer orders: create, read, pay, cancel, and add items.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class OrdersController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Creates a new instance of <see cref="OrdersController"/>.
    /// </summary>
    public OrdersController(ISender sender) => _sender = sender;

    /// <summary>
    /// Creates a new draft order for the authenticated user.
    /// </summary>
    [HttpPost]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Orders)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
    {
        var command = new CreateOrderCommand(GetUserId(), request.Items);

        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            value => CreatedAtAction(nameof(GetOrder), new { id = value.Id }, value),
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Returns a single order by ID. The user must own the order or be an admin.
    /// </summary>
    [HttpGet("{id:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetOrder(Guid id)
    {
        var result = await _sender.Send(new GetOrderQuery(id));

        return result.Match<IActionResult>(
            ok => Ok(ok),
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Returns a paged list of orders. Admins see all orders; customers see only their own.
    /// </summary>
    [HttpGet]
    [Authorize]
    [ProducesResponseType(typeof(PagedOrdersDto), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetOrders([FromQuery] int skip = 0, [FromQuery] int take = 20)
    {
        var isAdmin = User.IsInRole(RoleConstants.Admin);

        var result = await _sender.Send(new GetOrdersQuery(GetUserId(), isAdmin, skip, take));

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Adds an item to an existing draft order.
    /// </summary>
    [HttpPost("{id:guid}/items")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Orders)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AddOrderItem(Guid id, [FromBody] AddOrderItemRequest request)
    {
        var command = new AddOrderItemCommand(id, request.ProductId, request.Quantity);

        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Initiates payment for an order. Reserves products and creates a payment request.
    /// </summary>
    [HttpPost("{id:guid}/pay")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Payment)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> PayOrder(Guid id)
    {
        var command = new PayOrderCommand(id, GetUserId());

        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Cancels an order (draft or reserved). Releases any reserved product quantities.
    /// </summary>
    [HttpPost("{id:guid}/cancel")]
    [Authorize]
    [EnableRateLimiting(RateLimitPolicies.Orders)]
    [ProducesResponseType(typeof(OrderDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CancelOrder(Guid id, [FromBody] string? reason = null)
    {
        var command = new CancelOrderCommand(id, GetUserId(), reason);

        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
