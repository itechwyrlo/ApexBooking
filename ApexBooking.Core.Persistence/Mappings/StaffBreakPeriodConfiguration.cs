using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class StaffBreakPeriodConfiguration : IEntityTypeConfiguration<StaffBreakPeriod>
    {
        public void Configure(EntityTypeBuilder<StaffBreakPeriod> builder)
        {
            builder.ToTable("staff_break_periods");

            builder.HasKey(rbp => rbp.StaffBreakPeriodId);

            builder.Property(rbp => rbp.StaffBreakPeriodId)
                .HasConversion(id => id.Value, v => new StaffBreakPeriodId(v))
                .HasColumnName("id");

            builder.Property(rbp => rbp.StaffAvailabilityScheduleId)
                .HasConversion(id => id.Value, v => new StaffAvailabilityScheduleId(v))
                .HasColumnName("staff_availability_schedule_id")
                .IsRequired();

            builder.Property(rbp => rbp.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(rbp => rbp.StaffId)
                .HasConversion(id => id.Value, v => new StaffId(v))
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
            builder.HasIndex(rbp => rbp.StaffAvailabilityScheduleId);
            builder.HasIndex(rbp => rbp.StaffId);
            builder.HasIndex(rbp => rbp.TenantId);
        }
    }
}
