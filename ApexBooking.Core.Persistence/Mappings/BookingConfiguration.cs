using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            builder.ToTable("bookings");

            builder.HasKey(b => b.BookingId);

            builder.Property(b => b.BookingId)
                .HasConversion(id => id.Value, v => new BookingId(v))
                .HasColumnName("id");

            builder.Property(b => b.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(b => b.BookingReference)
                .HasColumnName("booking_reference")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(b => b.ServiceId)
                .HasConversion(id => id.Value, v => new ServiceId(v))
                .HasColumnName("service_id")
                .IsRequired();

            builder.Property(b => b.ResourceId)
                .HasConversion(id => id.Value, v => new ResourceId(v))
                .HasColumnName("resource_id")
                .IsRequired();

            builder.Property(b => b.ScheduledDate)
                .HasColumnName("scheduled_date")
                .IsRequired();

            builder.Property(b => b.ScheduledStartTime)
                .HasColumnName("scheduled_start_time")
                .IsRequired();

            builder.Property(b => b.ScheduledEndTime)
                .HasColumnName("scheduled_end_time")
                .IsRequired();

            builder.Property(b => b.DurationMinutes)
                .HasColumnName("duration_minutes")
                .IsRequired();

            builder.Property(b => b.Status)
                .HasConversion<string>()
                .HasColumnName("status")
                .IsRequired();

            builder.Property(b => b.ConfirmationMode)
                .HasConversion<string>()
                .HasColumnName("confirmation_mode")
                .IsRequired();

            builder.Property(b => b.PriceSnapshot)
                .HasColumnName("price_snapshot")
                .HasPrecision(12, 2)
                .IsRequired();

            builder.Property(b => b.CurrencyCode)
                .HasColumnName("currency_code")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();

            builder.Property(b => b.CustomerNotes)
                .HasColumnName("customer_notes")
                .HasMaxLength(2000);

            builder.Property(b => b.CancellationReason)
                .HasColumnName("cancellation_reason")
                .HasMaxLength(500);

            builder.Property(b => b.CancelledAt)
                .HasColumnName("cancelled_at");

            builder.Property(b => b.CancelledByUserId)
                .HasColumnName("cancelled_by_user_id");

            builder.Property(b => b.RescheduledFromBookingId)
                .HasConversion(id => id!.Value, v => new BookingId(v))
                .HasColumnName("rescheduled_from_booking_id");

            builder.Property(b => b.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(b => b.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // Relationships
            builder.HasMany(b => b.StatusLogs)
                .WithOne()
                .HasForeignKey(bsl => bsl.BookingId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes per ERD
            // Index: (tenant_id, resource_id, scheduled_date, status) for slot availability queries
            builder.HasIndex(b => new { b.TenantId, b.ResourceId, b.ScheduledDate, b.Status });

            // Index: (tenant_id, scheduled_date) for calendar queries
            builder.HasIndex(b => new { b.TenantId, b.ScheduledDate });

            // Unique constraint on booking_reference
            builder.HasIndex(b => b.BookingReference).IsUnique();
        }
    }
}
