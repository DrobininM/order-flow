namespace OrderService.Application.Common.Interfaces;

public interface IEmailService
{
    Task SendAsync(string email, string subject, string body, CancellationToken ct = default);
}
