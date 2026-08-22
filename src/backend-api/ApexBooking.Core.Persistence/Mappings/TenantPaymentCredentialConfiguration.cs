using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ApexBooking.Core.Persistence.Mappings;

public class TenantPaymentCredentialConfiguration : IEntityTypeConfiguration<TenantPaymentCredential>
{
    private readonly ISecretProtector _secretProtector;

    public TenantPaymentCredentialConfiguration(ISecretProtector secretProtector)
    {
        _secretProtector = secretProtector;
    }

    public void Configure(EntityTypeBuilder<TenantPaymentCredential> builder)
    {
        builder.ToTable("TenantPaymentCredentials");
        builder.HasKey(c => c.TenantPaymentCredentialId);
         builder.Property(c => c.TenantPaymentCredentialId)
                .HasConversion(
                    id => id.Value,
                    value => new TenantPaymentCredentialId(value))
                .IsRequired();

        // SecretKey/WebhookSecret are encrypted at rest (ISecretProtector, backed by ASP.NET Core
        // Data Protection — AES-256-CBC + HMAC authenticated encryption). PublicKey stays plaintext:
        // it's the publishable key, safe to display/expose. Max length raised from 500 to 1000 —
        // Data Protection ciphertext (base64 + key-id header + auth tag) runs meaningfully larger
        // than the ~70-char raw PayMongo keys.
        var secretConverter = new ValueConverter<string, string>(
            plaintext => _secretProtector.Protect(plaintext),
            ciphertext => _secretProtector.Unprotect(ciphertext));

        var nullableSecretConverter = new ValueConverter<string?, string?>(
            plaintext => plaintext == null ? null : _secretProtector.Protect(plaintext),
            ciphertext => ciphertext == null ? null : _secretProtector.Unprotect(ciphertext));

        builder.Property(c => c.SecretKey)
            .HasConversion(secretConverter)
            .HasMaxLength(1000)
            .IsRequired();

        builder.Property(c => c.PublicKey).HasMaxLength(500).IsRequired();

        builder.Property(c => c.WebhookSecret)
            .HasConversion(nullableSecretConverter)
            .HasMaxLength(1000);

        builder.Property(c => c.IsEnabled).IsRequired();
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.UpdatedAt).IsRequired();
    }
}
