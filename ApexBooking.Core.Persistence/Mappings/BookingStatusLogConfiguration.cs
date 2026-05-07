using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class BookingStatusLogConfiguration : IEntityTypeConfiguration<BookingStatusLog>
    {
        public void Configure(EntityTypeBuilder<BookingStatusLog> builder)
        {
            builder.ToTable("booking_status_logs");

            builder.HasKey(bsl => bsl.BookingStatusLogId);

            builder.Property(bsl => bsl.BookingStatusLogId)
                .HasConversion(id => id.Value, v => new BookingStatusLogId(v))
                .HasColumnName("id");

            builder.Property(bsl => bsl.BookingId)
                .HasConversion(id => id.Value, v => new BookingId(v))
                .HasColumnName("booking_id")
                .IsRequired();

            builder.Property(bsl => bsl.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(bsl => bsl.PreviousStatus)
                .HasConversion<string>()
                .HasColumnName("previous_status");

            builder.Property(bsl => bsl.NewStatus)
                .HasConversion<string>()
                .HasColumnName("new_status")
                .IsRequired();

            builder.Property(bsl => bsl.ChangedByUserId)
                .HasColumnName("changed_by_user_id");

            builder.Property(bsl => bsl.Reason)
                .HasColumnName("change_reason")
                .HasMaxLength(500);

            builder.Property(bsl => bsl.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // Indexes
            builder.HasIndex(bsl => bsl.BookingId);
            builder.HasIndex(bsl => bsl.TenantId);
            builder.HasIndex(bsl => new { bsl.BookingId, bsl.CreatedAt });
        }
    }
}
