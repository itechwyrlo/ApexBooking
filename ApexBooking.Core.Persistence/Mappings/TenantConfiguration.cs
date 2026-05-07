using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("tenant");

            builder.HasKey(t => t.TenantId);

            builder.Property(t => t.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v));

            builder.Property(t => t.Slug).HasColumnName("slug").IsRequired();
            builder.Property(t => t.BusinessName).HasColumnName("business_name");
            builder.Property(t => t.OwnerFullName).HasColumnName("owner_full_name");
            builder.Property(t => t.OwnerEmail).HasColumnName("owner_email");
            builder.Property(t => t.OwnerPhone).HasColumnName("owner_phone");

            builder.Property(t => t.Status)
                .HasConversion<string>()
                .HasColumnName("status");

            builder.Property(t => t.CreatedAt).HasColumnName("created_at");
            builder.Property(t => t.UpdatedAt).HasColumnName("updated_at");
            builder.Property(t => t.DeactivatedAt).HasColumnName("deactivated_at");

            // User->Tenant relationship is configured in DbContext to prevent shadow property

            builder.HasMany(x => x.Users)
            .WithOne()
            .HasForeignKey(x => x.TenantId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);
            // 1:1
            builder.HasOne(t => t.TenantProfile)
                .WithOne(p => p.Tenant)
                .HasForeignKey<TenantProfile>(p => p.TenantId)
                .IsRequired();

            builder.HasOne(t => t.TenantSettings)
                .WithOne(s => s.Tenant)
                .HasForeignKey<TenantSettings>(s => s.TenantId)
                .IsRequired();

            builder.HasIndex(t => t.Slug).IsUnique();
        }
    }
}
