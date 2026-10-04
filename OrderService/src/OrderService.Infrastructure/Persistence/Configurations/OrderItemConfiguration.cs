using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using OrderService.Domain.Entities;

namespace OrderService.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems");

        builder.HasKey(i => i.Id);

        builder.Property(i => i.OrderId).IsRequired();
        builder.Property(i => i.ProductId).IsRequired();
        builder.Property(i => i.ProductName).HasMaxLength(200).IsRequired();
        
        builder.Property(i => i.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();
        
        builder.Property(i => i.Currency)
            .HasConversion<string>()
            .HasMaxLength(6)
            .IsRequired();
        
        builder.Property(i => i.Quantity).IsRequired();

        builder.Ignore(i => i.TotalPrice);
    }
}
