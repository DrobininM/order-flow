using ErrorOr;
using Microsoft.AspNetCore.Mvc;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// Base helper for mapping ErrorOr errors to IActionResult.
/// </summary>
public static class ControllerErrorHelper
{
    /// <summary>
    /// Maps the first error from an ErrorOr result to an <see cref="IActionResult"/>.
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
