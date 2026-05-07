using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class ServiceResourceConfiguration : IEntityTypeConfiguration<ServiceResource>
    {
        public void Configure(EntityTypeBuilder<ServiceResource> builder)
        {
            builder.ToTable("service_resources");

            builder.HasKey(sr => sr.ServiceResourceId);

            builder.Property(sr => sr.ServiceResourceId)
                .HasConversion(id => id.Value, v => new ServiceResourceId(v))
                .HasColumnName("id");

            builder.Property(sr => sr.ServiceId)
                .HasConversion(id => id.Value, v => new ServiceId(v))
                .HasColumnName("service_id")
                .IsRequired();

            builder.Property(sr => sr.ResourceId)
                .HasConversion(id => id.Value, v => new ResourceId(v))
                .HasColumnName("resource_id")
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
            builder.HasIndex(sr => new { sr.ServiceId, sr.ResourceId })
                .IsUnique();

            // Indexes
            builder.HasIndex(sr => sr.ServiceId);
            builder.HasIndex(sr => sr.ResourceId);
            builder.HasIndex(sr => sr.TenantId);
        }
    }
}
