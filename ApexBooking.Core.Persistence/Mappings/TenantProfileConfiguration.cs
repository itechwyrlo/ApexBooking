using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class TenantProfileConfiguration : IEntityTypeConfiguration<TenantProfile>
    {
        public void Configure(EntityTypeBuilder<TenantProfile> builder)
        {
            builder.ToTable("tenant_profile");

            builder.HasKey(x => x.TenantProfileId);

            builder.Property(x => x.TenantProfileId)
                .HasConversion(id => id.Value, v => new TenantProfileId(v));

            builder.Property(x => x.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v));

            builder.Property(x => x.Timezone).HasColumnName("timezone");
            builder.Property(x => x.CurrencyCode).HasColumnName("currency_code");

            builder.Property(x => x.CreatedAt).HasColumnName("created_at");
            builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
        }
    }
}
