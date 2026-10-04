using System.Security.Claims;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PaymentService.Application.Payments.DTOs;
using PaymentService.Application.Payments.Queries;

namespace PaymentService.Presentation.Controllers;

/// <summary>
/// Read access to payments. Payments are only visible to the user who owns them.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class PaymentsController : ControllerBase
{
    private readonly ISender _sender;

    /// <summary>
    /// Creates a new instance of <see cref="PaymentsController"/>.
    /// </summary>
    public PaymentsController(ISender sender) => _sender = sender;

    /// <summary>
    /// Returns the payment for an order. The authenticated user must own the order.
    /// </summary>
    [HttpGet("order/{orderId:guid}")]
    [Authorize]
    [ProducesResponseType(typeof(PaymentDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetPaymentByOrder(Guid orderId)
    {
        var result = await _sender.Send(new GetPaymentQuery(orderId, GetUserId()));

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    private Guid GetUserId() =>
        Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
}
