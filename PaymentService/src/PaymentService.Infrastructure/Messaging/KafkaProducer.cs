using Confluent.Kafka;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PaymentService.Application.Common.Interfaces;

namespace PaymentService.Infrastructure.Messaging;

public sealed class KafkaProducer : IMessageProducer, IAsyncDisposable
{
    private readonly IProducer<string, string> _producer;
    private readonly ILogger<KafkaProducer> _logger;

    public KafkaProducer(IOptions<KafkaOptions> options, ILogger<KafkaProducer> logger)
    {
        var config = new ProducerConfig
        {
            BootstrapServers = options.Value.BootstrapServers,
            Acks = Acks.All,
            EnableIdempotence = true,
            MessageTimeoutMs = 10_000,
        };

        _producer = new ProducerBuilder<string, string>(config).Build();
        _logger = logger;
    }

    public async Task PublishAsync(string topic, string key, string message, CancellationToken ct = default)
    {
        try
        {
            var result = await _producer.ProduceAsync(topic, new Message<string, string>
            {
                Key = key,
                Value = message
            }, ct);

            _logger.LogInformation(
                "Published message to {Topic} [{Partition}] @ {Offset}",
                result.Topic, result.Partition, result.Offset.Value);
        }
        catch (ProduceException<string, string> ex)
        {
            _logger.LogError(ex, "Failed to publish message to {Topic}: {Reason}", topic, ex.Error.Reason);
            throw;
        }
    }

    public async ValueTask DisposeAsync()
    {
        await Task.Run(() => _producer.Flush());

        _producer.Dispose();
    }
}
