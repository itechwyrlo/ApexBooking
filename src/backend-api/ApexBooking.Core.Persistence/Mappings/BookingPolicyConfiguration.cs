using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class BookingPolicyConfiguration : IEntityTypeConfiguration<BookingPolicy>
    {
        public void Configure(EntityTypeBuilder<BookingPolicy> builder)
        {
            builder.ToTable("booking_policy");

            builder.HasKey(p => p.BookingPolicyId);

            builder.Property(p => p.BookingPolicyId)
                .HasConversion(id => id.Value, v => new BookingPolicyId(v))
                .ValueGeneratedNever()
                .HasColumnName("booking_policy_id");

            builder.Property(p => p.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(p => p.BookingConfirmationMode)
                .HasConversion<string>()
                .HasColumnName("booking_confirmation_mode")
                .IsRequired();

            builder.Property(p => p.MinAdvanceBookingHours)
                .HasColumnName("min_advance_booking_hours")
                .IsRequired();

            builder.Property(p => p.MaxAdvanceBookingDays)
                .HasColumnName("max_advance_booking_days")
                .IsRequired();

            builder.Property(p => p.CancellationCutoffHours)
                .HasColumnName("cancellation_cutoff_hours")
                .IsRequired();

            builder.Property(p => p.LateCancellationPolicy)
                .HasConversion<string>()
                .HasColumnName("late_cancellation_policy")
                .IsRequired();

            builder.Property(p => p.NotifyBookingConfirmed)
                .HasColumnName("notify_booking_confirmed")
                .IsRequired();

            builder.Property(p => p.NotifyBookingCancelled)
                .HasColumnName("notify_booking_cancelled")
                .IsRequired();

            builder.Property(p => p.NotifyBookingReminder)
                .HasColumnName("notify_booking_reminder")
                .IsRequired();

            builder.Property(p => p.NotifyNewCustomer)
                .HasColumnName("notify_new_customer")
                .IsRequired();

            builder.Property(p => p.ReminderHoursBefore)
                .HasColumnName("reminder_hours_before")
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasIndex(p => p.TenantId).IsUnique();
        }
    }
}
