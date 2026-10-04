namespace OrderService.Application.Common.Interfaces;

public interface IJwtTokenService
{
    string GenerateAccessToken(Guid userId, string email, string role);
    string GenerateRefreshToken();
    (Guid userId, string email, string role) ValidateAccessToken(string token);
}
