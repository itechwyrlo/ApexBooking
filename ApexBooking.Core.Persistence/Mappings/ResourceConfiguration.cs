using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
    {
        public void Configure(EntityTypeBuilder<Resource> builder)
        {
            builder.ToTable("resources");

            builder.HasKey(r => r.ResourceId);

            builder.Property(r => r.ResourceId)
                .HasConversion(id => id.Value, v => new ResourceId(v))
                .HasColumnName("id");

            builder.Property(r => r.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(r => r.Name)
                .HasColumnName("name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(r => r.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

            builder.Property(r => r.ResourceType)
                .HasConversion<string>()
                .HasColumnName("resource_type")
                .IsRequired();

            builder.Property(r => r.Capacity)
                .HasColumnName("capacity")
                .HasDefaultValue(1)
                .IsRequired();

            builder.Property(r => r.IsActive)
                .HasColumnName("is_active")
                .HasDefaultValue(true)
                .IsRequired();

            builder.Property(r => r.CreatedAt)
                .HasColumnName("created_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            builder.Property(r => r.UpdatedAt)
                .HasColumnName("updated_at")
                .HasDefaultValueSql("GETUTCDATE()")
                .IsRequired();

            // Relationships
            builder.HasMany(r => r.AvailabilitySchedules)
                .WithOne()
                .HasForeignKey(ras => ras.ResourceId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.AvailabilityExceptions)
                .WithOne()
                .HasForeignKey(rae => rae.ResourceId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(r => r.TenantId);
            builder.HasIndex(r => new { r.TenantId, r.IsActive });
            builder.HasIndex(r => r.ResourceType);
        }
    }
}
