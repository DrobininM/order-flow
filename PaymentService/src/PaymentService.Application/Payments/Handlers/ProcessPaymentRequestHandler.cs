using System.Text.Json;
using Microsoft.Extensions.Logging;
using PaymentService.Application.Common.Interfaces;
using PaymentService.Application.IntegrationEvents;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Domain.Gateways;
using PaymentService.Domain.Repositories;

namespace PaymentService.Application.Payments.Handlers;

/// <summary>
/// Handles a <c>payment.requested</c> integration event: registers the payment, calls the
/// appropriate gateway and enqueues the payment result as an outbox message.
/// Consumption is idempotent through the inbox table.
/// </summary>
public sealed class ProcessPaymentRequestHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    /// <summary>
    /// Payment method is not part of the request event yet; all requests are treated as card payments.
    /// </summary>
    private const PaymentMethod DefaultPaymentMethod = PaymentMethod.CreditCard;

    private readonly IPaymentGatewayFactory _gatewayFactory;
    private readonly IPaymentRepository _paymentRepository;
    private readonly IInboxMessageRepository _inboxRepository;
    private readonly IOutboxMessageRepository _outboxRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ProcessPaymentRequestHandler> _logger;

    public ProcessPaymentRequestHandler(
        IPaymentGatewayFactory gatewayFactory,
        IPaymentRepository paymentRepository,
        IInboxMessageRepository inboxRepository,
        IOutboxMessageRepository outboxRepository,
        IUnitOfWork unitOfWork,
        ILogger<ProcessPaymentRequestHandler> logger)
    {
        _gatewayFactory = gatewayFactory;
        _paymentRepository = paymentRepository;
        _inboxRepository = inboxRepository;
        _outboxRepository = outboxRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    /// <summary>
    /// Processes a payment request message. Invalid payloads are acknowledged and skipped so
    /// that a malformed message cannot block the consumer group indefinitely.
    /// </summary>
    public async Task HandleAsync(string messageId, string payload, CancellationToken ct)
    {
        if (await _inboxRepository.ExistsAsync(messageId, ct))
        {
            _logger.LogInformation("Message {MessageId} already processed. Skipping.", messageId);

            return;
        }

        var inboxMessage = InboxMessage.Create(messageId, nameof(PaymentRequestedIntegrationEvent), payload);
        await _inboxRepository.AddAsync(inboxMessage, ct);

        var request = JsonSerializer.Deserialize<PaymentRequestedIntegrationEvent>(payload, JsonOptions);

        if (request is null)
        {
            _logger.LogWarning("Payment request message {MessageId} could not be deserialized.", messageId);

            inboxMessage.MarkAsProcessed();
            await _unitOfWork.SaveChangesAsync(ct);

            return;
        }

        if (!Enum.TryParse<Currency>(request.Currency, true, out var currency))
        {
            _logger.LogWarning("Payment request message {MessageId} references an unsupported currency.", messageId);

            inboxMessage.MarkAsProcessed();
            await _unitOfWork.SaveChangesAsync(ct);

            return;
        }

        var paymentResult = Payment.Create(
            request.OrderId, request.UserId, request.TotalAmount, currency, DefaultPaymentMethod);

        if (paymentResult.IsError)
        {
            _logger.LogWarning(
                "Payment request message {MessageId} was rejected: {Error}.",
                messageId, paymentResult.FirstError.Code);

            inboxMessage.MarkAsProcessed();
            await _unitOfWork.SaveChangesAsync(ct);

            return;
        }

        var payment = paymentResult.Value;
        await _paymentRepository.AddAsync(payment, ct);

        payment.MarkAsProcessing();

        var gateway = _gatewayFactory.GetGateway(payment.Currency, payment.Method);

        var gatewayRequest = new PaymentRequest(
            payment.Amount,
            payment.Currency,
            payment.Method,
            $"Payment for order {payment.OrderId}",
            new Dictionary<string, string> { ["OrderId"] = payment.OrderId.ToString() });

        var result = await gateway.ProcessPaymentAsync(gatewayRequest, ct);

        if (result.IsSuccess)
        {
            payment.MarkAsSucceeded(result.ExternalPaymentId!);

            await EnqueueOutboxAsync(
                new PaymentSucceededIntegrationEvent(payment.OrderId, result.ExternalPaymentId!, DateTime.UtcNow), ct);

            _logger.LogInformation("Payment {PaymentId} for order {OrderId} succeeded.", payment.Id, payment.OrderId);
        }
        else
        {
            payment.MarkAsFailed(result.ErrorMessage!);

            await EnqueueOutboxAsync(
                new PaymentFailedIntegrationEvent(payment.OrderId, result.ErrorMessage!, DateTime.UtcNow), ct);

            _logger.LogWarning("Payment {PaymentId} for order {OrderId} failed.", payment.Id, payment.OrderId);
        }

        inboxMessage.MarkAsProcessed();
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private async Task EnqueueOutboxAsync(IIntegrationEvent integrationEvent, CancellationToken ct)
    {
        var payload = JsonSerializer.Serialize(integrationEvent, integrationEvent.GetType());

        await _outboxRepository.AddAsync(OutboxMessage.Create(integrationEvent.GetType().Name, payload), ct);
    }
}
