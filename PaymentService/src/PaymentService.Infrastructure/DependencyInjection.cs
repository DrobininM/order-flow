using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Domain.Gateways;
using PaymentService.Domain.Repositories;
using PaymentService.Infrastructure.BackgroundServices;
using PaymentService.Infrastructure.DomainEvents;
using PaymentService.Infrastructure.Gateways;
using PaymentService.Infrastructure.Messaging;
using PaymentService.Infrastructure.Persistence;
using PaymentService.Infrastructure.Persistence.Repositories;
using PaymentService.Infrastructure.Resilience;

namespace PaymentService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");

        // EF Core
        services.AddDbContext<PaymentDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        // Repositories
        services.AddScoped<IPaymentRepository, PaymentRepository>();
        services.AddScoped<IInboxMessageRepository, InboxMessageRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IDomainEventEntryRepository, DomainEventEntryRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<DomainEventSerializer>();
        services.AddScoped<DomainEventPublisher>();

        // Payment Gateways
        services.AddSingleton<IPaymentGateway, MockPaymentGateway>();
        services.AddSingleton<IPaymentGatewayFactory, PaymentGatewayFactory>();

        // Kafka
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));
        services.AddSingleton<IMessageProducer, KafkaProducer>();
        services.AddSingleton<KafkaMessageRouter>();

        // Background Services
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.Configure<DomainEventRecoveryOptions>(
            configuration.GetSection(DomainEventRecoveryOptions.SectionName));
        services.AddHostedService<KafkaConsumerService>();
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<DomainEventRecoveryService>();

        // Resilience
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));
        services.AddResiliencePolicies();

        return services;
    }
}
