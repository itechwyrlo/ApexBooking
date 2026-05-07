using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Domain.Entities;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
    {
        public void Configure(EntityTypeBuilder<RefreshToken> builder)
        {
            builder.ToTable("refresh_token");

            builder.HasKey(rt => rt.Id);

            builder.Property(rt => rt.Id)
                .HasConversion(id => id.Value, v => new RefreshTokenId(v))
                .HasColumnName("id");

            builder.Property(rt => rt.UserId).HasColumnName("user_id").IsRequired();

            builder.Property(rt => rt.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(rt => rt.Token)
                .HasColumnName("token")
                .HasMaxLength(512)
                .IsRequired();

            builder.Property(rt => rt.IsUsed).HasColumnName("is_used");
            builder.Property(rt => rt.IsRevoked).HasColumnName("is_revoked");

            builder.Property(rt => rt.CreatedAt).HasColumnName("created_at");
            builder.Property(rt => rt.ExpiryDate).HasColumnName("expiry_date");

            builder.HasIndex(rt => rt.Token).IsUnique();
            builder.HasIndex(rt => rt.UserId);
            builder.HasIndex(rt => rt.TenantId);
        }
    }
}
