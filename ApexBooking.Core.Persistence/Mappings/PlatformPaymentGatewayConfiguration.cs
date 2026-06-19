using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBooking.Core.Persistence.Mappings;

public class PlatformPaymentGatewayConfiguration : IEntityTypeConfiguration<PlatformPaymentGateway>
{
    public void Configure(EntityTypeBuilder<PlatformPaymentGateway> builder)
    {
        builder.ToTable("platform_payment_gateways");

        builder.HasKey(g => g.PlatformPaymentGatewayId);

        builder.Property(g => g.PlatformPaymentGatewayId)
            .HasConversion(id => id.Value, v => new PlatformPaymentGatewayId(v))
            .HasColumnName("id");

        builder.Property(g => g.GatewayProvider)
            .HasConversion<string>()
            .HasColumnName("gateway_provider")
            .IsRequired();

        builder.Property(g => g.ClientId)
            .HasColumnName("client_id")
            .IsRequired();

        builder.Property(g => g.SecretKeyEncrypted)
            .HasColumnName("secret_key_encrypted")
            .IsRequired();

        builder.Property(g => g.WebhookId)
            .HasColumnName("webhook_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(g => g.Mode)
            .HasConversion<string>()
            .HasColumnName("mode")
            .IsRequired();

        builder.Property(g => g.IsActive)
            .HasColumnName("is_active")
            .IsRequired();

        builder.Property(g => g.ValidatedAt)
            .HasColumnName("validated_at");

        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(g => g.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.HasIndex(g => g.IsActive)
            .IsUnique()
            .HasFilter("[is_active] = 1");
    }
}
