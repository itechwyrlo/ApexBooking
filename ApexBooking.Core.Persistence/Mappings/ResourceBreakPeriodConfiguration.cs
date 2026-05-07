using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class ResourceBreakPeriodConfiguration : IEntityTypeConfiguration<ResourceBreakPeriod>
    {
        public void Configure(EntityTypeBuilder<ResourceBreakPeriod> builder)
        {
            builder.ToTable("resource_break_periods");

            builder.HasKey(rbp => rbp.ResourceBreakPeriodId);

            builder.Property(rbp => rbp.ResourceBreakPeriodId)
                .HasConversion(id => id.Value, v => new ResourceBreakPeriodId(v))
                .HasColumnName("id");

            builder.Property(rbp => rbp.ResourceAvailabilityScheduleId)
                .HasConversion(id => id.Value, v => new ResourceAvailabilityScheduleId(v))
                .HasColumnName("resource_availability_schedule_id")
                .IsRequired();

            builder.Property(rbp => rbp.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(rbp => rbp.ResourceId)
                .HasConversion(id => id.Value, v => new ResourceId(v))
                .HasColumnName("resource_id")
                .IsRequired();

            builder.Property(rbp => rbp.BreakStartTime)
                .HasColumnName("break_start");

            builder.Property(rbp => rbp.BreakEndTime)
                .HasColumnName("break_end");

            builder.Property(rbp => rbp.Label)
                .HasColumnName("label")
                .HasMaxLength(100);

            builder.Property(rbp => rbp.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // Indexes
            builder.HasIndex(rbp => rbp.ResourceAvailabilityScheduleId);
            builder.HasIndex(rbp => rbp.ResourceId);
            builder.HasIndex(rbp => rbp.TenantId);
        }
    }
}
