using OrderService.Domain.Common;
using OrderService.Domain.Enums;
using OrderService.Domain.ValueObjects;

namespace OrderService.Domain.Entities;

/// <summary>
/// Represents a registered user.
/// </summary>
public sealed class User : AggregateRoot<Guid>
{
    private readonly List<RefreshToken> _refreshTokens = [];

    /// <summary>
    /// User's email address.
    /// </summary>
    public Email Email { get; private set; } = null!;

    /// <summary>
    /// Hashed password.
    /// </summary>
    public string PasswordHash { get; private set; } = null!;

    /// <summary>
    /// User role (e.g. Customer, Admin).
    /// </summary>
    public UserRole Role { get; private set; }

    /// <summary>
    /// When the account was created.
    /// </summary>
    public DateTime CreatedAt { get; private set; }

    /// <summary>
    /// Active refresh tokens for this user.
    /// </summary>
    public IReadOnlyList<RefreshToken> RefreshTokens => _refreshTokens.AsReadOnly();

    private User() { } // EF Core

    private User(Guid id, Email email, string passwordHash, UserRole role) : base(id)
    {
        Email = email;
        PasswordHash = passwordHash;
        Role = role;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Creates a new user account.
    /// </summary>
    /// <param name="email">Validated email.</param>
    /// <param name="passwordHash">Hashed password. Must not be empty.</param>
    /// <param name="role">User role. Defaults to Customer.</param>
    /// <exception cref="DomainException">Thrown if passwordHash is empty.</exception>
    public static User Create(Email email, string passwordHash, UserRole role = UserRole.Customer)
    {
        if (string.IsNullOrWhiteSpace(passwordHash))
            throw new DomainException(ErrorCodes.UserEmptyPassword, "Password hash cannot be empty.");

        return new User(Guid.NewGuid(), email, passwordHash, role);
    }

    /// <summary>
    /// Adds a new refresh token for this user.
    /// </summary>
    /// <param name="token">Opaque token string. Must not be empty.</param>
    /// <param name="expiresAt">Expiration time.</param>
    /// <exception cref="DomainException">Thrown if token is empty.</exception>
    /// <returns>The created refresh token.</returns>
    public RefreshToken AddRefreshToken(string token, DateTime expiresAt)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new DomainException(ErrorCodes.TokenEmpty, "Refresh token cannot be empty.");

        var refreshToken = RefreshToken.Create(Id, token, expiresAt);
        _refreshTokens.Add(refreshToken);

        return refreshToken;
    }

    /// <summary>
    /// Revokes all active refresh tokens for this user.
    /// Used when a suspicious token reuse is detected.
    /// </summary>
    public void RevokeAllRefreshTokens()
    {
        foreach (var rt in _refreshTokens)
            rt.Revoke();
    }

    /// <summary>
    /// Updates the password hash. Typically used after a password reset.
    /// </summary>
    /// <param name="newPasswordHash">New hashed password. Must not be empty.</param>
    /// <exception cref="DomainException">Thrown if newPasswordHash is empty.</exception>
    public void UpdatePasswordHash(string newPasswordHash)
    {
        if (string.IsNullOrWhiteSpace(newPasswordHash))
            throw new DomainException(ErrorCodes.UserEmptyPassword, "Password hash cannot be empty.");

        PasswordHash = newPasswordHash;
    }
}
