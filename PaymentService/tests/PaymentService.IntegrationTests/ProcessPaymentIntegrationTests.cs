using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using PaymentService.Application.IntegrationEvents;
using PaymentService.Application.Payments.Handlers;
using PaymentService.Domain.Entities;
using PaymentService.Domain.Enums;
using PaymentService.Infrastructure.Persistence;

namespace PaymentService.IntegrationTests;

public sealed class ProcessPaymentIntegrationTests : IClassFixture<PaymentServiceFixture>
{
    private readonly PaymentServiceFixture _fixture;

    public ProcessPaymentIntegrationTests(PaymentServiceFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task ProcessPayment_WithSuccessfulGateway_PersistsPaymentResultAndOutbox()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();

        await ProcessAsync(messageId, BuildPayload(orderId, userId, 42.5m));

        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        var payment = await dbContext.Payments.SingleAsync(p => p.OrderId == orderId);
        Assert.Equal(userId, payment.UserId);
        Assert.Equal(42.5m, payment.Amount);
        Assert.Equal(Currency.USD, payment.Currency);
        Assert.Equal(PaymentStatus.Succeeded, payment.Status);
        Assert.Equal(StubPaymentGateway.ExternalPaymentId, payment.ExternalPaymentId);
        Assert.NotNull(payment.CompletedAt);

        var inbox = await dbContext.InboxMessages.SingleAsync(m => m.MessageId == messageId);
        Assert.Equal(nameof(PaymentRequestedIntegrationEvent), inbox.Type);
        Assert.NotNull(inbox.ProcessedAt);

        var outbox = await FindOutboxForOrderAsync(dbContext, orderId);
        Assert.Equal(nameof(PaymentSucceededIntegrationEvent), outbox.Type);
        Assert.Null(outbox.ProcessedAt);

        var integrationEvent = JsonSerializer.Deserialize<PaymentSucceededIntegrationEvent>(outbox.Payload);
        Assert.NotNull(integrationEvent);
        Assert.Equal(orderId, integrationEvent.OrderId);
        Assert.Equal(StubPaymentGateway.ExternalPaymentId, integrationEvent.ExternalPaymentId);

        // PaymentCreatedDomainEvent + PaymentSucceededDomainEvent, both dispatched and marked processed.
        // Payload is jsonb, so the filter is applied in memory.
        var domainEvents = (await dbContext.DomainEventEntries.ToListAsync())
            .Where(e => e.Payload.Contains(orderId.ToString(), StringComparison.Ordinal))
            .ToList();
        Assert.Equal(2, domainEvents.Count);
        Assert.All(domainEvents, e => Assert.NotNull(e.ProcessedAt));
    }

    [Fact]
    public async Task ProcessPayment_WithDuplicateMessage_IsIdempotent()
    {
        var orderId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid().ToString();
        var payload = BuildPayload(orderId, userId, 10m);

        await ProcessAsync(messageId, payload);
        await ProcessAsync(messageId, payload);

        using var scope = _fixture.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();

        Assert.Equal(1, await dbContext.Payments.CountAsync(p => p.OrderId == orderId));
        Assert.Equal(1, await dbContext.InboxMessages.CountAsync(m => m.MessageId == messageId));

        // Payload is jsonb, so the filter is applied in memory.
        var outboxMessages = await dbContext.OutboxMessages.ToListAsync();
        Assert.Equal(1, outboxMessages.Count(o => o.Payload.Contains(orderId.ToString(), StringComparison.Ordinal)));
    }

    private async Task ProcessAsync(string messageId, string payload)
    {
        using var scope = _fixture.Services.CreateScope();
        var handler = scope.ServiceProvider.GetRequiredService<ProcessPaymentRequestHandler>();

        await handler.HandleAsync(messageId, payload, CancellationToken.None);
    }

    private static string BuildPayload(Guid orderId, Guid userId, decimal amount) =>
        JsonSerializer.Serialize(new PaymentRequestedIntegrationEvent(
            orderId,
            userId,
            amount,
            Currency.USD.ToString(),
            [new PaymentRequestItemDto(Guid.NewGuid(), "Widget", 1, amount)],
            DateTime.UtcNow));

    private static async Task<OutboxMessage> FindOutboxForOrderAsync(PaymentDbContext dbContext, Guid orderId)
    {
        var messages = await dbContext.OutboxMessages.ToListAsync();
        var matches = messages
            .Where(m => m.Payload.Contains(orderId.ToString(), StringComparison.Ordinal))
            .ToList();

        return Assert.Single(matches);
    }
}
