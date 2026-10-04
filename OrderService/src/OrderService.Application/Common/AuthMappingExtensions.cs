using Microsoft.Extensions.Options;
using OrderService.Application.Auth.DTOs;
using OrderService.Domain.Entities;

namespace OrderService.Application.Common;

/// <summary>
/// Builds <see cref="AuthResponse"/> from User and tokens, using configured expiration values.
/// </summary>
public static class AuthMappingExtensions
{
    /// <summary>
    /// Creates an AuthResponse with access and refresh token expiration derived from configuration.
    /// </summary>
    public static AuthResponse ToAuthResponse(
        this User user,
        string accessToken,
        string refreshToken,
        AuthOptions authOptions)
    {
        return new AuthResponse(
            accessToken,
            refreshToken,
            DateTime.UtcNow.AddMinutes(authOptions.AccessTokenExpirationMinutes),
            user.Id,
            user.Email.Value,
            user.Role.ToString());
    }
}
