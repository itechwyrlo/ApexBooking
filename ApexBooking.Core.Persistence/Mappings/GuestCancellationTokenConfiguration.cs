using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings;

public class GuestCancellationTokenConfiguration : IEntityTypeConfiguration<GuestCancellationToken>
{
    public void Configure(EntityTypeBuilder<GuestCancellationToken> builder)
    {
        builder.ToTable("guest_cancellation_tokens");

        builder.HasKey(gct => gct.TokenId);

        builder.Property(gct => gct.TokenId)
            .HasConversion(id => id.Value, v => new GuestCancellationTokenId(v))
            .HasColumnName("id");

        builder.Property(gct => gct.GuestId)
            .HasConversion(id => id.Value, v => new GuestId(v))
            .HasColumnName("guest_id")
            .IsRequired();

        builder.Property(gct => gct.TenantId)
            .HasConversion(id => id.Value, v => new TenantId(v))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(gct => gct.TokenHash)
            .HasColumnName("token_hash")
            .HasMaxLength(64)
            .IsRequired();

        builder.Property(gct => gct.ExpiresAt)
            .HasColumnName("expires_at")
            .IsRequired();

        builder.Property(gct => gct.IsUsed)
            .HasColumnName("is_used")
            .HasDefaultValue(false)
            .IsRequired();

        builder.Property(gct => gct.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        // GuestCancellationToken → Guest: FK on GuestCancellationToken.GuestId
        builder.HasOne(gct => gct.Guest)
            .WithOne(g => g.CancellationToken)
            .HasForeignKey<GuestCancellationToken>(gct => gct.GuestId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(gct => gct.TokenHash).IsUnique();
        builder.HasIndex(gct => gct.GuestId).IsUnique();
    }
}
