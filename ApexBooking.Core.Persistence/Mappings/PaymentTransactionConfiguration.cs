using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings;

public class PaymentTransactionConfiguration : IEntityTypeConfiguration<PaymentTransaction>
{
    public void Configure(EntityTypeBuilder<PaymentTransaction> builder)
    {
        builder.ToTable("payment_transactions");

        builder.HasKey(pt => pt.PaymentTransactionId);

        builder.Property(pt => pt.PaymentTransactionId)
            .HasConversion(id => id.Value, v => new PaymentTransactionId(v))
            .HasColumnName("id");

        builder.Property(pt => pt.TenantId)
            .HasConversion(id => id.Value, v => new TenantId(v))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(pt => pt.BookingId)
            .HasConversion(id => id.Value, v => new BookingId(v))
            .HasColumnName("booking_id")
            .IsRequired();

        builder.Property(pt => pt.GatewayProvider)
            .HasConversion<string>()
            .HasColumnName("gateway_provider")
            .IsRequired();

        builder.Property(pt => pt.GatewayTransactionId)
            .HasColumnName("gateway_transaction_id")
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(pt => pt.Amount)
            .HasColumnName("amount")
            .HasPrecision(12, 2)
            .IsRequired();

        builder.Property(pt => pt.CurrencyCode)
            .HasColumnName("currency_code")
            .HasMaxLength(3)
            .IsFixedLength()
            .IsRequired();

        builder.Property(pt => pt.Status)
            .HasConversion<string>()
            .HasColumnName("status")
            .IsRequired();

        builder.Property(pt => pt.PaymentMethodType)
            .HasColumnName("payment_method_type")
            .HasMaxLength(50);

        builder.Property(pt => pt.PaymentMethodLast4)
            .HasColumnName("payment_method_last4")
            .HasMaxLength(4)
            .IsFixedLength();

        builder.Property(pt => pt.PaidAt)
            .HasColumnName("paid_at");

        builder.Property(pt => pt.FailureReason)
            .HasColumnName("failure_reason");

        builder.Property(pt => pt.CreatedAt)
            .HasColumnName("created_at")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.Property(pt => pt.UpdatedAt)
            .HasColumnName("updated_at")
            .HasDefaultValueSql("GETUTCDATE()")
            .IsRequired();

        builder.HasIndex(pt => pt.BookingId).IsUnique();
        builder.HasIndex(pt => pt.GatewayTransactionId).IsUnique();
        builder.HasIndex(pt => pt.TenantId);
    }
}