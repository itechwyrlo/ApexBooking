using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class ServiceStaffConfiguration : IEntityTypeConfiguration<ServiceStaff>
    {
        public void Configure(EntityTypeBuilder<ServiceStaff> builder)
        {
            builder.ToTable("service_staffs");

            builder.HasKey(sr => sr.ServiceStaffId);

            builder.Property(sr => sr.ServiceStaffId)
                .HasConversion(id => id.Value, v => new ServiceStaffId(v))
                .HasColumnName("id");

            builder.Property(sr => sr.ServiceId)
                .HasConversion(id => id.Value, v => new ServiceId(v))
                .HasColumnName("service_id")
                .IsRequired();

            builder.Property(sr => sr.StaffId)
                .HasConversion(id => id.Value, v => new StaffId(v))
                .HasColumnName("staff_id")
                .IsRequired();

            builder.Property(sr => sr.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(sr => sr.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // Unique constraint: (service_id, resource_id)
            builder.HasIndex(sr => new { sr.ServiceId, sr.StaffId })
                .IsUnique();

            // Indexes
            builder.HasIndex(sr => sr.ServiceId);
            builder.HasIndex(sr => sr.StaffId);
            builder.HasIndex(sr => sr.TenantId);
        }
    }
}
