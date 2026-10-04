using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using PaymentService.Application;
using PaymentService.Domain.Gateways;
using PaymentService.Infrastructure;
using PaymentService.Infrastructure.Persistence;
using Testcontainers.PostgreSql;

namespace PaymentService.IntegrationTests;

public sealed class PaymentServiceFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres =
        new PostgreSqlBuilder("postgres:16-alpine").Build();

    public ServiceProvider Services { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString(),
                ["Kafka:BootstrapServers"] = "localhost:9092"
            })
            .Build();

        var services = new ServiceCollection();

        services.AddLogging();
        services.AddApplication();
        services.AddInfrastructure(configuration);

        // Replace the random mock gateway with a deterministic stub.
        services.RemoveAll<IPaymentGateway>();
        services.AddSingleton<IPaymentGateway, StubPaymentGateway>();

        foreach (var descriptor in services
                     .Where(d => d.ServiceType == typeof(IHostedService))
                     .ToList())
        {
            services.Remove(descriptor);
        }

        Services = services.BuildServiceProvider();

        using var scope = Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
        await dbContext.Database.EnsureCreatedAsync();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();
}
