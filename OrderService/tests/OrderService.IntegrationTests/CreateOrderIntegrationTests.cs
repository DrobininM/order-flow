using System.Text.Json;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Application.IntegrationEvents;
using OrderService.Application.Orders.Commands;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Persistence;

namespace OrderService.IntegrationTests;

public sealed class CreateOrderIntegrationTests : IClassFixture<OrderServiceFixture>
{
    private readonly OrderServiceFixture _fixture;

    public CreateOrderIntegrationTests(OrderServiceFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task CreateOrder_WithAvailableProduct_PersistsOrderAndOutboxMessage()
    {
        var userId = Guid.NewGuid();
        Guid productId;

        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var product = Product.Create(
                "Integration Test Product",
                "Seeded for the happy-path test",
                Money.Create(12.5m, Currency.USD).Value,
                stockQuantity: 5).Value;

            dbContext.Products.Add(product);
            await dbContext.SaveChangesAsync();

            productId = product.Id;
        }

        Guid orderId;

        using (var scope = _fixture.Services.CreateScope())
        {
            var sender = scope.ServiceProvider.GetRequiredService<ISender>();

            var response = await sender.Send(
                new CreateOrderCommand(userId, [new CreateOrderItem(productId, 2)]));

            Assert.False(response.IsError);
            orderId = response.Value.Id;
        }

        using (var scope = _fixture.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var order = await dbContext.Orders
                .Include(o => o.Items)
                .SingleAsync(o => o.Id == orderId);

            Assert.Equal(userId, order.UserId);
            Assert.Equal(OrderStatus.Draft, order.Status);

            var item = Assert.Single(order.Items);
            Assert.Equal(productId, item.ProductId);
            Assert.Equal(2, item.Quantity);
            Assert.Equal(25m, order.GetTotalAmount().Amount);

            var outboxMessage = await dbContext.OutboxMessages.SingleAsync();
            Assert.Equal(nameof(OrderCreatedIntegrationEvent), outboxMessage.Type);
            Assert.Null(outboxMessage.ProcessedAt);

            var integrationEvent =
                JsonSerializer.Deserialize<OrderCreatedIntegrationEvent>(outboxMessage.Payload);
            
            Assert.NotNull(integrationEvent);
            Assert.Equal(orderId, integrationEvent.OrderId);
            Assert.Equal(25m, integrationEvent.TotalAmount);

            Assert.Equal(1, await dbContext.DomainEventEntries.CountAsync());
        }
    }
}
