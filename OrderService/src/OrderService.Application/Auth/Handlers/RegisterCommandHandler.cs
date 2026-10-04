using ErrorOr;
using MediatR;
using Microsoft.Extensions.Options;
using OrderService.Application.Auth.Commands;
using OrderService.Application.Auth.DTOs;
using OrderService.Application.Common;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Common;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;

namespace OrderService.Application.Auth.Handlers;

public sealed class RegisterCommandHandler : IRequestHandler<RegisterCommand, ErrorOr<AuthResponse>>
{
    private readonly IUserRepository _userRepository;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IOptions<AuthOptions> _authOptions;

    public RegisterCommandHandler(
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

    public async Task<ErrorOr<AuthResponse>> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        var emailResult = Email.Create(request.Email);
        if (emailResult.IsError)
            return emailResult.Errors;

        if (await _userRepository.IsExistByEmailAsync(emailResult.Value, cancellationToken))
            return Error.Conflict(ErrorCodes.UserExists, "User with this email already exists.");

        var passwordHash = _passwordHasher.Hash(request.Password);

        var user = User.Create(emailResult.Value, passwordHash, UserRole.Customer);

        await _userRepository.AddAsync(user, cancellationToken);

        var accessToken = _jwtTokenService.GenerateAccessToken(user.Id, user.Email.Value, user.Role.ToString());
        var refreshToken = _jwtTokenService.GenerateRefreshToken();

        var refreshExpiresAt = DateTime.UtcNow.AddDays(_authOptions.Value.RefreshTokenExpirationDays);
        user.AddRefreshToken(refreshToken, refreshExpiresAt);
        
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return user.ToAuthResponse(accessToken, refreshToken, _authOptions.Value);
    }
}
