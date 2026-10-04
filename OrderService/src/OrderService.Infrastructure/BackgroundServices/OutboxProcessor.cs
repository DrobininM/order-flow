using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Application.Common.Interfaces;
using OrderService.Domain.Repositories;

namespace OrderService.Infrastructure.BackgroundServices;

public sealed class OutboxProcessor : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxOptions _options;
    private const int BatchSize = 20;
    private static readonly TimeSpan ProcessingDelay = TimeSpan.FromSeconds(5);

    public OutboxProcessor(
        IServiceScopeFactory scopeFactory,
        ILogger<OutboxProcessor> logger,
        IOptions<OutboxOptions> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("OutboxProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessOutboxAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing outbox messages.");
            }

            await Task.Delay(ProcessingDelay, stoppingToken);
        }
    }

    // Duplicate messages can be sent to Kafka if the message was sent, but SaveChanges failed.
    // Therefore, consumers should check message.id for idempotence.
    private async Task ProcessOutboxAsync(CancellationToken ct)
    {
        using var scope = _scopeFactory.CreateScope();
        var outboxRepository = scope.ServiceProvider.GetRequiredService<IOutboxMessageRepository>();
        var kafkaProducer = scope.ServiceProvider.GetRequiredService<IMessageProducer>();
        var router = scope.ServiceProvider.GetRequiredService<Messaging.KafkaMessageRouter>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        await unitOfWork.BeginTransactionAsync(ct);

        try
        {
            var messages =
                await outboxRepository.GetUnprocessedWithLockAsync(BatchSize, _options.MaxRetryCount, ct);

            foreach (var message in messages)
            {
                try
                {
                    var topic = router.GetDestinationTopic(message.Type);
                    await kafkaProducer.PublishAsync(topic, message.Id.ToString(), message.Payload, ct);

                    message.MarkAsProcessed();

                    _logger.LogInformation("Outbox message {Id} of type {Type} processed.", message.Id, message.Type);
                }
                catch (Exception ex)
                {
                    message.MarkAsFailed(ex.Message);
                    _logger.LogError(ex, "Failed to process outbox message {Id}.", message.Id);
                }
            }

            if (messages.Count > 0)
                await unitOfWork.SaveChangesAsync(ct);

            await unitOfWork.CommitTransactionAsync(ct);
        }
        catch
        {
            await unitOfWork.RollbackTransactionAsync(ct);

            throw;
        }
    }
}
