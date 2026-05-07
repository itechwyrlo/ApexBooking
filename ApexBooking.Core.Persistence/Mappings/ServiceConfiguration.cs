using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class ServiceConfiguration : IEntityTypeConfiguration<Service>
    {
        public void Configure(EntityTypeBuilder<Service> builder)
        {
            builder.ToTable("services");

            builder.HasKey(s => s.ServiceId);

            builder.Property(s => s.ServiceId)
                .HasConversion(id => id.Value, v => new ServiceId(v))
                .HasColumnName("id");

            builder.Property(s => s.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(s => s.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(s => s.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            builder.Property(s => s.DurationMinutes)
                .HasColumnName("duration_minutes")
                .IsRequired();

            builder.Property(s => s.BufferBeforeMinutes)
                .HasColumnName("buffer_before_minutes")
                .HasDefaultValue(0);

            builder.Property(s => s.BufferAfterMinutes)
                .HasColumnName("buffer_after_minutes")
                .HasDefaultValue(0);

            builder.Property(s => s.Price)
                .HasColumnName("price")
                .HasPrecision(12, 2)
                .HasDefaultValue(0.00m)
                .IsRequired();

            builder.Property(s => s.CurrencyCode)
                .HasColumnName("currency_code")
                .HasMaxLength(3)
                .IsFixedLength()
                .IsRequired();

            builder.Property(s => s.MinAdvanceBookingHours)
                .HasColumnName("min_advance_booking_hours");

            builder.Property(s => s.MaxAdvanceBookingDays)
                .HasColumnName("max_advance_booking_days");

            builder.Property(s => s.CancellationPolicyOverride)
                .HasConversion<string>()
                .HasColumnName("cancellation_policy_override");

            builder.Property(s => s.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(s => s.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(s => s.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // Relationships
            builder.HasMany(s => s.ServiceResources)
                .WithOne()
                .HasForeignKey(sr => sr.ServiceId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(s => s.TenantId);
            builder.HasIndex(s => new { s.TenantId, s.IsActive });
            builder.HasIndex(s => new { s.TenantId, s.Name });
        }
    }
}
