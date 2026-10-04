using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.Application.Payments.Handlers;

namespace PaymentService.Infrastructure.Messaging;

/// <summary>
/// Consumes <c>payment.requested</c> integration events and dispatches them to
/// <see cref="ProcessPaymentRequestHandler"/>. Failed messages are seeked back for retry;
/// idempotency is enforced by the handler through the inbox table.
/// </summary>
public sealed class KafkaConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaConsumerService> _logger;
    private readonly KafkaOptions _options;
    private readonly string[] _topics = [KafkaTopicNames.PaymentRequested];

    public KafkaConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<KafkaConsumerService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var config = new ConsumerConfig
        {
            BootstrapServers = _options.BootstrapServers,
            GroupId = _options.GroupId,
            AutoOffsetReset = AutoOffsetReset.Earliest,
            EnableAutoCommit = false
        };

        using var consumer = new ConsumerBuilder<string, string>(config).Build();
        consumer.Subscribe(_topics);

        _logger.LogInformation("Kafka consumer started. Listening to: {Topics}", string.Join(", ", _topics));

        while (!stoppingToken.IsCancellationRequested)
        {
            ConsumeResult<string, string>? result = null;

            try
            {
                result = consumer.Consume(stoppingToken);

                var messageId = result.Message.Key
                    ?? $"{result.Topic}-{result.Partition.Value}-{result.Offset.Value}";

                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ProcessPaymentRequestHandler>();

                await handler.HandleAsync(messageId, result.Message.Value, stoppingToken);

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming payment request message.");

                if (result is not null)
                {
                    try
                    {
                        consumer.Seek(result.TopicPartitionOffset);
                    }
                    catch (Exception seekEx)
                    {
                        _logger.LogError(seekEx, "Failed to seek back to offset {Offset}.", result.Offset.Value);
                    }
                }
            }
        }

        consumer.Close();
    }
}
