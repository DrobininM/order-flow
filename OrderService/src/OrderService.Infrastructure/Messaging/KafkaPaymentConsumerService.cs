using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderService.Application.Payments.Handlers;

namespace OrderService.Infrastructure.Messaging;

/// <summary>
/// Consumes payment outcome events (<c>payment.succeeded</c> / <c>payment.failed</c>)
/// and applies them to the corresponding orders idempotently.
/// </summary>
public sealed class KafkaPaymentConsumerService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<KafkaPaymentConsumerService> _logger;
    private readonly KafkaOptions _options;
    
    private readonly string[] _topics =
    [
        KafkaTopicNames.PaymentSucceeded,
        KafkaTopicNames.PaymentFailed
    ];

    public KafkaPaymentConsumerService(
        IServiceScopeFactory scopeFactory,
        IOptions<KafkaOptions> options,
        ILogger<KafkaPaymentConsumerService> logger)
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
                var paymentSucceeded = result.Topic == KafkaTopicNames.PaymentSucceeded;

                using var scope = _scopeFactory.CreateScope();
                var handler = scope.ServiceProvider.GetRequiredService<ProcessPaymentResultHandler>();

                await handler.HandleAsync(messageId, paymentSucceeded, result.Message.Value, stoppingToken);

                consumer.Commit(result);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error consuming payment result message.");

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
