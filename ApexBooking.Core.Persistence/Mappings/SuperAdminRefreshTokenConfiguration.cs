using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBooking.Core.Persistence.Mappings;

public class SuperAdminRefreshTokenConfiguration : IEntityTypeConfiguration<SuperAdminRefreshToken>
{
    public void Configure(EntityTypeBuilder<SuperAdminRefreshToken> builder)
    {
        builder.ToTable("super_admin_refresh_token");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasConversion(id => id.Value, v => new SuperAdminRefreshTokenId(v));

        builder.Property(x => x.SuperAdminId).HasColumnName("super_admin_id");
        builder.Property(x => x.Token).HasColumnName("token").IsRequired();
        builder.Property(x => x.IsUsed).HasColumnName("is_used");
        builder.Property(x => x.IsRevoked).HasColumnName("is_revoked");
        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.ExpiryDate).HasColumnName("expiry_date");

        builder.HasIndex(x => x.Token).IsUnique();
    }
}
