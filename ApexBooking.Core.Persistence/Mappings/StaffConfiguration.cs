using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class StaffConfiguration : IEntityTypeConfiguration<Staff>
    {
        public void Configure(EntityTypeBuilder<Staff> builder)
        {
            builder.ToTable("Staffs");

            builder.HasKey(r => r.StaffId);

            builder.Property(r => r.StaffId)
                .HasConversion(id => id.Value, v => new StaffId(v))
                .HasColumnName("id");

            builder.Property(r => r.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(r => r.FirstName)
                .HasColumnName("first_name")
                .HasMaxLength(255)
                .IsRequired();
            builder.Property(r => r.LastName)
                .HasColumnName("last_name")
                .HasMaxLength(255)
                .IsRequired();

            builder.Property(r => r.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .IsRequired(false);
            
            builder.Property(r => r.ContactNumber)
                .HasColumnName("contact_number")
                .HasMaxLength(255)
                .IsRequired(false);

            builder.Property(r => r.Description)
                .HasColumnName("description")
                .HasMaxLength(1000);

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
                .HasForeignKey(ras => ras.StaffId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasMany(r => r.AvailabilityExceptions)
                .WithOne()
                .HasForeignKey(rae => rae.StaffId)
                .IsRequired()
                .OnDelete(DeleteBehavior.Cascade);

            // Indexes
            builder.HasIndex(r => r.TenantId);
            builder.HasIndex(r => new { r.TenantId, r.IsActive });
        }
    }
}
