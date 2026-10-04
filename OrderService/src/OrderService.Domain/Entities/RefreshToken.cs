using OrderService.Domain.Common;

namespace OrderService.Domain.Entities;

/// <summary>
/// Represents a refresh token for JWT authentication.
/// </summary>
public sealed class RefreshToken : Entity<Guid>
{
    /// <summary>
    /// ID of the user who owns this token.
    /// </summary>
    public Guid UserId { get; private set; }

    /// <summary>
    /// The opaque token string.
    /// </summary>
    public string Token { get; private set; } = null!;

    /// <summary>
    /// When the token expires.
    /// </summary>
    public DateTime ExpiresAt { get; private set; }

    /// <summary>
    /// Whether the token has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// When the token was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    private RefreshToken() { } // EF Core

    private RefreshToken(Guid id, Guid userId, string token, DateTime expiresAt) : base(id)
    {
        UserId = userId;
        Token = token;
        ExpiresAt = expiresAt;
        IsRevoked = false;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new refresh token after validation.
    /// </summary>
    /// <param name="userId">Owner user ID.</param>
    /// <param name="token">Opaque token string. Must not be empty.</param>
    /// <param name="expiresAt">Expiration time.</param>
    /// <exception cref="DomainException">Thrown if token is null or whitespace.</exception>
    public static RefreshToken Create(Guid userId, string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException(ErrorCodes.TokenEmpty, "Refresh token cannot be empty.");

        return new RefreshToken(Guid.NewGuid(), userId, token, expiresAt);
    }

    /// <summary>
    /// Returns true if the token is neither revoked nor expired.
    /// </summary>
    public bool IsValid() => !IsRevoked && ExpiresAt > DateTime.UtcNow;

    /// <summary>
    /// Revokes the token so it can no longer be used.
    /// </summary>
    public void Revoke() => IsRevoked = true;
}
