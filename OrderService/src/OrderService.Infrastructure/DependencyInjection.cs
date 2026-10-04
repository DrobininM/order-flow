using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Repositories;
using OrderService.Infrastructure.Auth;
using OrderService.Infrastructure.BackgroundServices;
using OrderService.Infrastructure.Caching;
using OrderService.Infrastructure.DomainEvents;
using OrderService.Infrastructure.Email;
using OrderService.Infrastructure.Messaging;
using OrderService.Infrastructure.Persistence;
using OrderService.Infrastructure.Persistence.Repositories;
using OrderService.Infrastructure.Resilience;
using StackExchange.Redis;

namespace OrderService.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var postgresConnectionString = configuration.GetConnectionString("Postgres")
            ?? throw new InvalidOperationException("Connection string 'Postgres' is not configured.");
        var redisConnectionString = configuration.GetConnectionString("Redis")
            ?? throw new InvalidOperationException("Connection string 'Redis' is not configured.");

        // EF Core
        services.AddDbContext<OrderDbContext>(options =>
            options.UseNpgsql(postgresConnectionString));

        // Redis
        services.AddSingleton<IConnectionMultiplexer>(_ =>
            ConnectionMultiplexer.Connect(redisConnectionString));

        services.AddSingleton<ICacheService, RedisCacheService>();

        // Auth
        services.AddSingleton<IPasswordHasher, BCryptPasswordHasher>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();

        // Repositories
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<IOutboxMessageRepository, OutboxMessageRepository>();
        services.AddScoped<IDomainEventEntryRepository, DomainEventEntryRepository>();
        services.AddScoped<IInboxMessageRepository, InboxMessageRepository>();

        // Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<DomainEventSerializer>();
        services.AddScoped<DomainEventPublisher>();

        // Email
        services.Configure<EmailOptions>(configuration.GetSection(EmailOptions.SectionName));
        services.AddSingleton<IEmailService, MockEmailService>();
        //services.AddSingleton<IEmailService, HttpEmailService>();

        // Kafka
        services.Configure<KafkaOptions>(configuration.GetSection(KafkaOptions.SectionName));

        services.AddSingleton<IMessageProducer, KafkaProducer>();
        services.AddSingleton<KafkaMessageRouter>();

        // Background Services
        services.Configure<OutboxOptions>(configuration.GetSection(OutboxOptions.SectionName));
        services.AddHostedService<KafkaPaymentConsumerService>();
        services.AddHostedService<OutboxProcessor>();
        services.AddHostedService<ReservationTimeoutProcessor>();
        services.AddHostedService<DomainEventRecoveryService>();

        // Resilience
        services.Configure<ResilienceOptions>(configuration.GetSection(ResilienceOptions.SectionName));
        services.AddResiliencePolicies();

        return services;
    }
}