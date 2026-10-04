namespace OrderService.Infrastructure.Email;

/// <summary>
/// Configuration for the HTTP email service.
/// </summary>
public sealed class EmailOptions
{
    public const string SectionName = "Email";

    public string BaseUrl { get; init; } = string.Empty;
}
