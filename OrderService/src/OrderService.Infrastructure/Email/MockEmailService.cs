using Microsoft.Extensions.Logging;
using OrderService.Application.Common.Interfaces;

namespace OrderService.Infrastructure.Email;

public sealed class MockEmailService : IEmailService
{
    private readonly ILogger<MockEmailService> _logger;

    public MockEmailService(ILogger<MockEmailService> logger) => _logger = logger;

    public Task SendAsync(string email, string subject, string body, CancellationToken ct = default)
    {
        _logger.LogInformation(
            "[EMAIL] To: {To}, Subject: {Subject}, Body: {Body}",
            email, subject, body);

        return Task.CompletedTask;
    }
}
