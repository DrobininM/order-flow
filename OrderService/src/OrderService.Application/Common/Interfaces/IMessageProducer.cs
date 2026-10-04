namespace OrderService.Application.Common.Interfaces;

public interface IMessageProducer
{
    Task PublishAsync(string topic, string key, string message, CancellationToken ct = default);
}
