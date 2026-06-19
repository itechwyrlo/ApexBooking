using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class StaffAvailabilityScheduleConfiguration : IEntityTypeConfiguration<StaffAvailabilitySchedule>
    {
        public void Configure(EntityTypeBuilder<StaffAvailabilitySchedule> builder)
        {
            builder.ToTable("staff_availability_schedules");

            builder.HasKey(ras => ras.StaffAvailabilityScheduleId);

            builder.Property(ras => ras.StaffAvailabilityScheduleId)
                .HasConversion(id => id.Value, v => new StaffAvailabilityScheduleId(v))
                .HasColumnName("id");

            builder.Property(ras => ras.StaffId)
                .HasConversion(id => id.Value, v => new StaffId(v))
                .HasColumnName("staff_id")
                .IsRequired();

            builder.Property(ras => ras.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(ras => ras.DayOfWeek)
                .HasColumnName("day_of_week")
                .IsRequired();

            builder.Property(ras => ras.IsAvailable)
                .HasColumnName("is_available")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(ras => ras.StartTime)
                .HasColumnName("start_time");

            builder.Property(ras => ras.EndTime)
                .HasColumnName("end_time");

            builder.Property(ras => ras.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(ras => ras.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // Relationships
            builder.HasMany(ras => ras.BreakPeriods)
                .WithOne()
                .HasForeignKey(rbp => rbp.StaffAvailabilityScheduleId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Unique constraint: (resource_id, day_of_week)
            builder.HasIndex(ras => new { ras.StaffId, ras.DayOfWeek })
                .IsUnique();

            // Indexes
            builder.HasIndex(ras => ras.StaffId);
            builder.HasIndex(ras => ras.TenantId);
        }
    }
}
