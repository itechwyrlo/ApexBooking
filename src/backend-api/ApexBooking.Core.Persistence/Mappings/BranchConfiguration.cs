using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class BranchConfiguration : IEntityTypeConfiguration<Branch>
    {
        public void Configure(EntityTypeBuilder<Branch> builder)
        {
            builder.ToTable("branches");
            builder.HasKey(b => b.BranchId);

            builder.Property(b => b.BranchId)
                .HasConversion(id => id.Value, v => new BranchId(v))
                .ValueGeneratedNever()
                .HasColumnName("branch_id");

            builder.Property(b => b.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .IsRequired()
                .HasColumnName("tenant_id");

            builder.Property(b => b.BranchName).HasColumnName("branch_name").HasMaxLength(255).IsRequired();
            builder.Property(b => b.TimeZoneId).HasColumnName("time_zone_id").HasMaxLength(100).IsRequired();
            builder.Property(b => b.IsActive).HasColumnName("is_active").IsRequired();
            builder.Property(b => b.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(b => b.UpdatedAt).HasColumnName("updated_at").IsRequired();

            // Philippine address value object, unwrapped into flat columns
            builder.OwnsOne(b => b.Address, a =>
            {
                a.Property(p => p.Street).HasColumnName("street").HasMaxLength(255).IsRequired();
                a.Property(p => p.Barangay).HasColumnName("barangay").HasMaxLength(100);
                a.Property(p => p.City).HasColumnName("city_municipality").HasMaxLength(100).IsRequired();
                a.Property(p => p.Province).HasColumnName("province").HasMaxLength(100).IsRequired();
                a.Property(p => p.ZipCode).HasColumnName("zip_code").HasMaxLength(4).IsRequired();
                a.Property(p => p.Country).HasColumnName("country").HasMaxLength(50).IsRequired();
            });
            builder.Navigation(b => b.Address).IsRequired();

            // Per-branch 7-day operating hours matrix
            builder.OwnsMany(b => b.OperatingHours, hours =>
            {
                hours.ToTable("BranchOperatingHours");
                hours.WithOwner().HasForeignKey("BranchId");
                hours.HasKey("BranchId", "DayOfWeek");

                hours.Property(h => h.DayOfWeek).HasConversion<string>().HasMaxLength(15).IsRequired();
                hours.Property(h => h.StartTime).IsRequired();
                hours.Property(h => h.EndTime).IsRequired();
                hours.Property(h => h.IsOff).IsRequired();
            });

            builder.Metadata.FindNavigation(nameof(Branch.OperatingHours))?
                .SetPropertyAccessMode(PropertyAccessMode.Field);

            builder.HasIndex(b => new { b.TenantId, b.BranchName }).IsUnique();
        }
    }
}
