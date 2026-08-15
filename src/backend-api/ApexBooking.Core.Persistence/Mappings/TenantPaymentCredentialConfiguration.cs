using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ApexBooking.Core.Persistence.Mappings;

public class TenantPaymentCredentialConfiguration : IEntityTypeConfiguration<TenantPaymentCredential>
{
    public void Configure(EntityTypeBuilder<TenantPaymentCredential> builder)
    {
        builder.ToTable("TenantPaymentCredentials");
        builder.HasKey(c => c.TenantPaymentCredentialId);
         builder.Property(c => c.TenantPaymentCredentialId)
                .HasConversion(
                    id => id.Value,
                    value => new TenantPaymentCredentialId(value))
                .IsRequired();

        builder.Property(c => c.SecretKey).HasMaxLength(500).IsRequired();
        builder.Property(c => c.PublicKey).HasMaxLength(500).IsRequired();
        builder.Property(c => c.WebhookSecret).HasMaxLength(500);
        builder.Property(c => c.IsEnabled).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
    }
}
