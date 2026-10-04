using ErrorOr;
using MediatR;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Application.Auth.Commands;
using OrderService.Application.Auth.DTOs;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Common;
using OrderService.Domain.Repositories;

namespace OrderService.Application.Auth.Handlers;

/// <summary>
/// Handles refresh token rotation with reuse detection.
/// When a revoked or expired refresh token is presented, all user's refresh tokens are revoked
/// (indicating a potential token theft scenario).
/// </summary>
public sealed class RefreshTokenCommandHandler : IRequestHandler<RefreshTokenCommand, ErrorOr<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<AuthOptions> _authOptions;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions;
        _logger = logger;
    }

    public async Task<ErrorOr<AuthResponse>> Handle(RefreshTokenCommand request, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByRefreshTokenAsync(request.RefreshToken, cancellationToken);

        if (user is null)
            return Error.Unauthorized(ErrorCodes.AuthInvalidRefresh, "Invalid or expired refresh token.");

        var currentToken = user.RefreshTokens.FirstOrDefault(rt => rt.Token == request.RefreshToken);

        if (currentToken is null || !currentToken.IsValid())
        {
            // Reuse detection: if a revoked/expired token is used, revoke all tokens (potential theft)
            if (currentToken is not null && currentToken.IsRevoked)
            {
                _logger.LogWarning(
                    "Refresh token reuse detected for user {UserId}. Revoking all refresh tokens.",
                    user.Id);

                user.RevokeAllRefreshTokens();
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }

            return Error.Unauthorized(ErrorCodes.AuthInvalidRefresh, "Invalid or expired refresh token.");
        }

        currentToken.Revoke();

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email.Value, user.Role.ToString());
        var newRefreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshExpiresAt = DateTime.UtcNow.AddDays(_authOptions.Value.RefreshTokenExpirationDays);
        user.AddRefreshToken(newRefreshToken, refreshExpiresAt);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToAuthResponse(accessToken, newRefreshToken, _authOptions.Value);
    }
}
