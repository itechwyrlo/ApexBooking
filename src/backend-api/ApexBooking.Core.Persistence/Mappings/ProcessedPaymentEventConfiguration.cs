using ApexBooking.Core.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBooking.Core.Persistence.Mappings;

public class ProcessedPaymentEventConfiguration : IEntityTypeConfiguration<ProcessedPaymentEvent>
{
    public void Configure(EntityTypeBuilder<ProcessedPaymentEvent> builder)
    {
        builder.ToTable("processed_payment_events");
        builder.HasKey(e => e.Id);

        builder.Property(e => e.PayMongoEventId).HasColumnName("paymongo_event_id").HasMaxLength(100).IsRequired();
        builder.Property(e => e.TenantId).HasColumnName("tenant_id").IsRequired();
        builder.Property(e => e.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(e => e.ProcessedAt).HasColumnName("processed_at").IsRequired();

        // Idempotency safety net: even if two deliveries of the same event somehow race past
        // IProcessedPaymentEventStore.ExistsAsync's check, only one insert can win here — the
        // loser's SaveChanges throws, PayMongo retries, and the retry's ExistsAsync check then
        // finds the winner's row and returns the clean no-op path.
        builder.HasIndex(e => e.PayMongoEventId).IsUnique();
    }
}
