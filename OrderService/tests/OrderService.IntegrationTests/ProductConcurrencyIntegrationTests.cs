using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OrderService.Domain.Entities;
using OrderService.Domain.Enums;
using OrderService.Domain.Repositories;
using OrderService.Domain.ValueObjects;
using OrderService.Infrastructure.Persistence;

namespace OrderService.IntegrationTests;

public sealed class ProductConcurrencyIntegrationTests : IClassFixture<OrderServiceFixture>
{
    private readonly OrderServiceFixture _fixture;

    public ProductConcurrencyIntegrationTests(OrderServiceFixture fixture) => _fixture = fixture;

    [Fact]
    public async Task LockedProductLoad_PopulatesConcurrencyToken_AndDetectsConcurrentUpdate()
    {
        Guid productId;

        using (var seedScope = _fixture.Services.CreateScope())
        {
            var seedContext = seedScope.ServiceProvider.GetRequiredService<OrderDbContext>();

            var product = Product.Create(
                "Concurrency Product",
                "Seeded for the concurrency test",
                Money.Create(10m, Currency.USD).Value,
                stockQuantity: 10).Value;

            seedContext.Products.Add(product);
            await seedContext.SaveChangesAsync();

            productId = product.Id;
        }

        using var scopeA = _fixture.Services.CreateScope();
        var contextA = scopeA.ServiceProvider.GetRequiredService<OrderDbContext>();
        var productsA = scopeA.ServiceProvider.GetRequiredService<IProductRepository>();

        Product loadedProduct;

        await using (var transaction = await contextA.Database.BeginTransactionAsync())
        {
            loadedProduct = Assert.Single(await productsA.GetByIdsWithLockAsync([productId]));

            Assert.NotEqual(0u, loadedProduct.Version);

            await transaction.CommitAsync();
        }

        using (var scopeB = _fixture.Services.CreateScope())
        {
            var contextB = scopeB.ServiceProvider.GetRequiredService<OrderDbContext>();

            await contextB.Products
                .Where(p => p.Id == productId)
                .ExecuteUpdateAsync(setters => setters.SetProperty(p => p.StockQuantity, 1));
        }

        loadedProduct.MarkUnavailable();

        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => contextA.SaveChangesAsync());
    }
}
