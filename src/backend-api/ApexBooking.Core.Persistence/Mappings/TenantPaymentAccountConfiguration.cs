using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class TenantPaymentAccountConfiguration : IEntityTypeConfiguration<TenantPaymentAccount>
    {
        public void Configure(EntityTypeBuilder<TenantPaymentAccount> builder)
        {
            builder.ToTable("TenantPaymentAccounts");
            
            builder.HasKey(tpa => tpa.Id);

            builder.Property(tpa => tpa.Id)
                .HasConversion(
                    id => id.Value,
                    value => new TenantPaymentAccountId(value))
                .IsRequired();

            builder.Property(tpa => tpa.TenantId)
                .HasConversion(
                    id => id.Value,
                    value => new TenantId(value))
                .IsRequired();

            // 3. Property Constraints and Requirements
            builder.Property(tpa => tpa.Status)
                .HasConversion<string>() // Stores Enum as a readable string
                .HasMaxLength(30)
                .IsRequired();

            builder.Property(tpa => tpa.ProviderAccountReference)
                .HasMaxLength(255); // Estimated length for an external API token or ID (e.g., Stripe/PayPal)

            // 4. Audit Properties
            builder.Property(tpa => tpa.CreatedAt)
                .IsRequired();

            builder.Property(tpa => tpa.UpdatedAt)
                .IsRequired();
        }
    }
}
