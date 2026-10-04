using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PaymentService.Domain.Entities;

namespace PaymentService.Infrastructure.Persistence.Configurations;

public sealed class DomainEventEntryConfiguration : IEntityTypeConfiguration<DomainEventEntry>
{
    public void Configure(EntityTypeBuilder<DomainEventEntry> builder)
    {
        builder.ToTable("DomainEventEntries");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.EventType).HasMaxLength(300).IsRequired();
        builder.Property(e => e.Payload).HasColumnType("jsonb").IsRequired();
        builder.Property(e => e.OccurredOn).IsRequired();
        builder.Property(e => e.CreatedAt).IsRequired();
        builder.Property(e => e.ProcessedAt);
        builder.Property(e => e.Error);
        builder.Property(e => e.RetryCount).IsRequired();

        builder.HasIndex(e => new { e.ProcessedAt, e.CreatedAt });
    }
}
