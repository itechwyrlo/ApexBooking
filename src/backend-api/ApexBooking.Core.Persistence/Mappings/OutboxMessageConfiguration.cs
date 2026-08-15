using ApexBooking.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBooking.Core.Persistence.Mappings;

public class OutboxMessageConfiguration : IEntityTypeConfiguration<OutboxMessage>
{
    public void Configure(EntityTypeBuilder<OutboxMessage> builder)
    {
        builder.ToTable("outbox_messages");

        builder.HasKey(m => m.Id);

        builder.Property(m => m.Id)
            .HasColumnName("id");

        builder.Property(m => m.EventType)
            .HasColumnName("event_type")
            .HasMaxLength(500)
            .IsRequired();

        builder.Property(m => m.Payload)
            .HasColumnName("payload")
            .IsRequired();

        builder.Property(m => m.OccurredAtUtc)
            .HasColumnName("occurred_at_utc")
            .IsRequired();

        builder.Property(m => m.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(m => m.ProcessedAtUtc)
            .HasColumnName("processed_at_utc");

        builder.Property(m => m.RetryCount)
            .HasColumnName("retry_count")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(m => m.LastError)
            .HasColumnName("last_error")
            .HasMaxLength(2000);

        // Both entry points (immediate trigger's TryClaimAsync-by-id, and the recurring sweep's
        // GetPendingIdsAsync) filter on Status; the sweep also orders by OccurredAtUtc.
        builder.HasIndex(m => new { m.Status, m.OccurredAtUtc });
    }
}
