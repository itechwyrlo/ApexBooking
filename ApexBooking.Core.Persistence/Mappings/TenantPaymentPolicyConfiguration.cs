using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings;

public class TenantPaymentPolicyConfiguration : IEntityTypeConfiguration<TenantPaymentPolicy>
{
    public void Configure(EntityTypeBuilder<TenantPaymentPolicy> builder)
    {
        builder.ToTable("tenant_payment_policy");

        builder.HasKey(x => x.TenantPaymentPolicyId);

        builder.Property(x => x.TenantPaymentPolicyId)
            .HasConversion(id => id.Value, v => new TenantPaymentPolicyId(v));

        builder.Property(x => x.TenantId)
            .HasConversion(id => id.Value, v => new TenantId(v));

        builder.Property(x => x.PaymentRequired).HasColumnName("payment_required");
        builder.Property(x => x.DepositOnly).HasColumnName("deposit_only");

        builder.Property(x => x.DepositType)
            .HasConversion<string>()
            .HasColumnName("deposit_type");

        builder.Property(x => x.DepositValue)
            .HasColumnName("deposit_value")
            .HasColumnType("decimal(10,2)");

        builder.Property(x => x.RefundPercent)
            .HasColumnName("refund_percent")
            .HasColumnType("decimal(5,2)");

        builder.Property(x => x.CreatedAt).HasColumnName("created_at");
        builder.Property(x => x.UpdatedAt).HasColumnName("updated_at");
    }
}
