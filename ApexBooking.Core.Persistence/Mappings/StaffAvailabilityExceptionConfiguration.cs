using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class StaffAvailabilityExceptionConfiguration : IEntityTypeConfiguration<StaffAvailabilityException>
    {
        public void Configure(EntityTypeBuilder<StaffAvailabilityException> builder)
        {
            builder.ToTable("staff_availability_exceptions");

            builder.HasKey(rae => rae.StaffAvailabilityExceptionId);

            builder.Property(rae => rae.StaffAvailabilityExceptionId)
                .HasConversion(id => id.Value, v => new StaffAvailabilityExceptionId(v))
                .HasColumnName("id");

            builder.Property(rae => rae.StaffId)
                .HasConversion(id => id.Value, v => new StaffId(v))
                .HasColumnName("staff_id")
                .IsRequired();

            builder.Property(rae => rae.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(rae => rae.ExceptionDate)
                .HasColumnName("exception_date")
                .IsRequired();

            builder.Property(rae => rae.ExceptionType)
                .HasConversion<string>()
                .HasColumnName("type")
                .IsRequired();

            builder.Property(rae => rae.StartTime)
                .HasColumnName("start_time");

            builder.Property(rae => rae.EndTime)
                .HasColumnName("end_time");

            builder.Property(rae => rae.Note)
                .HasColumnName("note")
                .HasMaxLength(500);

            builder.Property(rae => rae.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(rae => rae.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // Unique constraint: (resource_id, exception_date, exception_type)
            builder.HasIndex(rae => new { rae.StaffId, rae.ExceptionDate, rae.ExceptionType })
                .IsUnique();

            // Indexes
            builder.HasIndex(rae => rae.StaffId);
            builder.HasIndex(rae => rae.ExceptionDate);
            builder.HasIndex(rae => rae.TenantId);
        }
    }
}
