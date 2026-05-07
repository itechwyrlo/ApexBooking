using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class TenantSettingsConfiguration : IEntityTypeConfiguration<TenantSettings>
    {
        public void Configure(EntityTypeBuilder<TenantSettings> builder)
        {
            builder.ToTable("tenant_setting");

            builder.HasKey(x => x.TenantSettingsId);

            builder.Property(x => x.TenantSettingsId)
                .HasConversion(id => id.Value, v => new TenantSettingsId(v));

            builder.Property(x => x.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v));

            builder.Property(x => x.BookingConfirmationMode)
                .HasConversion<string>();

            builder.Property(x => x.LateCancellationPolicy)
                .HasConversion<string>();

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}
