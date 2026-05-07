using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class ResourceAvailabilityExceptionConfiguration : IEntityTypeConfiguration<ResourceAvailabilityException>
    {
        public void Configure(EntityTypeBuilder<ResourceAvailabilityException> builder)
        {
            builder.ToTable("resource_availability_exceptions");

            builder.HasKey(rae => rae.ResourceAvailabilityExceptionId);

            builder.Property(rae => rae.ResourceAvailabilityExceptionId)
                .HasConversion(id => id.Value, v => new ResourceAvailabilityExceptionId(v))
                .HasColumnName("id");

            builder.Property(rae => rae.ResourceId)
                .HasConversion(id => id.Value, v => new ResourceId(v))
                .HasColumnName("resource_id")
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
            builder.HasIndex(rae => new { rae.ResourceId, rae.ExceptionDate, rae.ExceptionType })
                .IsUnique();

            // Indexes
            builder.HasIndex(rae => rae.ResourceId);
            builder.HasIndex(rae => rae.ExceptionDate);
            builder.HasIndex(rae => rae.TenantId);
        }
    }
}
