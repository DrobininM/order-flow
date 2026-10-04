using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace PaymentService.Presentation.Controllers;

/// <summary>
/// Maps ErrorOr errors to standard HTTP action results.
/// </summary>
public static class ControllerErrorHelper
{
    /// <summary>
    /// Maps an ErrorOr error to an <see cref="IActionResult"/> based on its type.
    /// </summary>
    public static IActionResult MapError(Error error) => error.Type switch
    {
        ErrorType.Validation => new BadRequestObjectResult(new { error.Code, error.Description }),
        ErrorType.NotFound => new NotFoundObjectResult(new { error.Code, error.Description }),
        ErrorType.Conflict => new ConflictObjectResult(new { error.Code, error.Description }),
        ErrorType.Unauthorized => new UnauthorizedObjectResult(new { error.Code, error.Description }),
        ErrorType.Forbidden => new ForbidResult(),
        _ => new ObjectResult(new { error.Code, error.Description }) { StatusCode = 500 }
    };
}
