using ErrorOr;
using MediatR;
using Microsoft.Extensions.Options;
using OrderService.Application.Auth.Commands;
using OrderService.Application.Auth.DTOs;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Common;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Auth.Handlers;

public sealed class LoginCommandHandler : IRequestHandler<LoginCommand, ErrorOr<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<AuthOptions> _authOptions;

    public LoginCommandHandler(
        IUserRepository userRepository,
        IJwtTokenService jwtTokenService,
        IPasswordHasher passwordHasher,
        IUnitOfWork unitOfWork,
        IOptions<AuthOptions> authOptions)
    {
        _userRepository = userRepository;
        _jwtTokenService = jwtTokenService;
        _passwordHasher = passwordHasher;
        _unitOfWork = unitOfWork;
        _authOptions = authOptions;
    }

    public async Task<ErrorOr<AuthResponse>> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsError)
            return emailResult.Errors;

        var user = await _userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);

        if (user is null)
            return Error.Unauthorized(ErrorCodes.AuthInvalid, "Invalid email or password.");

        if (!_passwordHasher.Verify(request.Password, user.PasswordHash))
            return Error.Unauthorized(ErrorCodes.AuthInvalid, "Invalid email or password.");

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email.Value, user.Role.ToString());
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshExpiresAt = DateTime.UtcNow.AddDays(_authOptions.Value.RefreshTokenExpirationDays);
        user.AddRefreshToken(refreshToken, refreshExpiresAt);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToAuthResponse(accessToken, refreshToken, _authOptions.Value);
    }
}
