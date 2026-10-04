using System.Net.Http.Json;
using Microsoft.Extensions.Options;
using OrderService.Application.Common.Interfaces;
using OrderService.Infrastructure.Resilience;

namespace OrderService.Infrastructure.Email;

public sealed class HttpEmailService : IEmailService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly EmailOptions _options;

    public HttpEmailService(IHttpClientFactory httpClientFactory, IOptions<EmailOptions> options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
    }

    public async Task SendAsync(string email, string subject, string body, CancellationToken ct = default)
    {
        var client = _httpClientFactory.CreateClient(HttpClientNames.Resilient);
        client.BaseAddress = new Uri(_options.BaseUrl);

        var response = await client.PostAsJsonAsync(
            "send",
            new SendEmailRequest(email, subject, body),
            ct);

        response.EnsureSuccessStatusCode();
    }

    private sealed record SendEmailRequest(string To, string Subject, string Body);
}
