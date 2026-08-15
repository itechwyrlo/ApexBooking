using System;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class BookingConfiguration : IEntityTypeConfiguration<Booking>
    {
        public void Configure(EntityTypeBuilder<Booking> builder)
        {
            // 1. Table and Primary Key Mapping
            builder.ToTable("bookings");
            builder.HasKey(b => b.BookingId);

            // 2. Strongly-Typed ID Value Object Conversions
            builder.Property(b => b.BookingId)
                .HasConversion(id => id.Value, v => new BookingId(v))
                .ValueGeneratedNever()
                .HasColumnName("booking_id");

            builder.Property(b => b.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .IsRequired()
                .HasColumnName("tenant_id");

            builder.Property(b => b.BranchId)
                .HasConversion(id => id.Value, v => new BranchId(v))
                .IsRequired()
                .HasColumnName("branch_id");

            builder.Property(b => b.CustomerId)
                .HasConversion(id => id.Value, v => new CustomerId(v))
                .IsRequired()
                .HasColumnName("customer_id");

            builder.Property(b => b.StaffId)
                .HasConversion(id => id.Value, v => new TenantMemberId(v))
                .IsRequired()
                .HasColumnName("staff_member_id");

            builder.Property(b => b.ServiceId)
                .HasConversion(id => id.Value, v => new ServiceId(v))
                .IsRequired()
                .HasColumnName("service_id");

            // 3. Structural Property Requirements
            builder.Property(b => b.BookingReference)
                .HasColumnName("booking_reference")
                .HasMaxLength(50)
                .IsRequired();

            builder.Property(b => b.ScheduledDate)
                .HasColumnName("scheduled_date")
                .IsRequired();

            builder.Property(b => b.ScheduledStartTime)
                .HasColumnName("scheduled_start_time")
                .IsRequired();

            builder.Property(b => b.DurationMinutes)
                .HasColumnName("duration_minutes")
                .IsRequired();

            builder.Property(b => b.BufferAfterMinutes)
                .HasColumnName("buffer_after_minutes")
                .IsRequired();

            builder.Property(b => b.CustomerNotes)
                .HasColumnName("customer_notes")
                .HasMaxLength(1000);

            builder.Property(b => b.StaffNotes)
                .HasColumnName("staff_notes")
                .HasMaxLength(1000);

            // 4. State Enum Mapping (Persisted cleanly as a string text representation)
            builder.Property(b => b.Status)
                .HasConversion<string>()
                .HasColumnName("status")
                .HasMaxLength(30)
                .IsRequired();

            // 4b. Payment Snapshot — captured once at booking time (see Booking.cs for rationale)
            builder.Property(b => b.RequiresUpfrontPayment)
                .HasColumnName("requires_upfront_payment")
                .IsRequired();

            builder.Property(b => b.AmountDue)
                .HasColumnName("amount_due")
                .HasPrecision(12, 2)
                .IsRequired();

            builder.Property(b => b.ServicePriceAtBooking)
                .HasColumnName("service_price_at_booking")
                .HasPrecision(12, 2);

            builder.Property(b => b.InVisitAmountCollected)
                .HasColumnName("in_visit_amount_collected")
                .HasPrecision(12, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            builder.Property(b => b.CurrencyCode)
                .HasColumnName("currency_code")
                .HasMaxLength(3)
                .IsRequired();

            builder.Property(b => b.PaymentConfirmedVia)
                .HasConversion<string>()
                .HasColumnName("payment_confirmed_via")
                .HasMaxLength(20);

            builder.Property(b => b.PayMongoPaymentId)
                .HasColumnName("paymongo_payment_id")
                .HasMaxLength(100);

            builder.Property(b => b.RefundStatus)
                .HasConversion<string>()
                .HasColumnName("refund_status")
                .HasMaxLength(20)
                .IsRequired();

            builder.Property(b => b.RefundedAmount)
                .HasColumnName("refunded_amount")
                .HasPrecision(12, 2);

            builder.Property(b => b.RefundedAt)
                .HasColumnName("refunded_at");

            // 5. Operations & Cancellation Audits Mapping
            builder.Property(b => b.CancellationReason).HasColumnName("cancellation_reason").HasMaxLength(500);
            builder.Property(b => b.CancelledAt).HasColumnName("cancelled_at");
            builder.Property(b => b.CancelledByUserId).HasColumnName("cancelled_by_user_id");
            builder.Property(b => b.ServiceCompletedAt).HasColumnName("service_completed_at");
            builder.Property(b => b.NoShowAt).HasColumnName("no_show_at");
            builder.Property(b => b.CheckedInAt).HasColumnName("checked_in_at");

            builder.Property(b => b.RescheduledFromBookingId)
                .HasConversion(id => id != null ? id.Value : (Guid?)null, v => v != null ? new BookingId(v.Value) : null)
                .HasColumnName("rescheduled_from_booking_id");

            // 6. Audit & Ignores
            builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(b => b.UpdatedAt).HasColumnName("updated_at").IsRequired();
            builder.Ignore(b => b.DomainEvents);

            // 7. Core Indexes (Optimizes dashboard read lists and slot collision check queries)
            builder.HasIndex(b => b.BookingReference).IsUnique();
            builder.HasIndex(b => new { b.TenantId, b.BranchId, b.ScheduledDate, b.StaffId });
            builder.HasIndex(b => new { b.TenantId, b.CustomerId }); // Powers per-customer booking history lookups
        }
    }
}
