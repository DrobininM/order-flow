using ErrorOr;
using MediatR;
using OrderService.Application.Auth.DTOs;

namespace OrderService.Application.Auth.Commands;

public sealed record RegisterCommand(string Email, string Password) : IRequest<ErrorOr<AuthResponse>>;

public sealed record LoginCommand(string Email, string Password) : IRequest<ErrorOr<AuthResponse>>;

public sealed record RefreshTokenCommand(string RefreshToken) : IRequest<ErrorOr<AuthResponse>>;
