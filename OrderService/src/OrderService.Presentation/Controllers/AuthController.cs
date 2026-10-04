using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using OrderService.Application.Auth.Commands;
using OrderService.Application.Auth.DTOs;
using OrderService.Presentation;

namespace OrderService.Presentation.Controllers;

/// <summary>
/// Handles authentication operations: register, login, and token refresh.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public sealed class AuthController : ControllerBase
{
    private readonly ISender _sender;

    public AuthController(ISender sender) => _sender = sender;

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    [HttpPost("register")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Register([FromBody] RegisterCommand command)
    {
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Authenticates a user and returns access + refresh tokens.
    /// </summary>
    [HttpPost("login")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login([FromBody] LoginCommand command)
    {
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }

    /// <summary>
    /// Refreshes an access token using a valid refresh token (rotation).
    /// </summary>
    [HttpPost("refresh")]
    [EnableRateLimiting(RateLimitPolicies.Auth)]
    [ProducesResponseType(typeof(AuthResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenCommand command)
    {
        var result = await _sender.Send(command);

        return result.Match<IActionResult>(
            Ok,
            errors => ControllerErrorHelper.MapError(errors[0]));
    }
}
