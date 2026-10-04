namespace OrderService.Application.Common;

/// <summary>
/// Configuration values needed by the Application layer for auth token lifetimes.
/// Bound from the "Jwt" section from settings.
/// </summary>
public sealed class AuthOptions
{
    /// <summary>
    /// How many minutes an access token is valid.
    /// </summary>
    public int AccessTokenExpirationMinutes { get; init; } = 15;

    /// <summary>
    /// How many days a refresh token is valid before it expires.
    /// </summary>
    public int RefreshTokenExpirationDays { get; init; } = 7;
}
