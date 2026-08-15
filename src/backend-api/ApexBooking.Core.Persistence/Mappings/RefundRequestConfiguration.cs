using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings;

public class RefundRequestConfiguration : IEntityTypeConfiguration<RefundRequest>
{
    public void Configure(EntityTypeBuilder<RefundRequest> builder)
    {
        builder.ToTable("refund_requests");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").ValueGeneratedNever();

        builder.Property(r => r.TenantId)
            .HasConversion(id => id!.Value, v => new TenantId(v))
            .HasColumnName("tenant_id")
            .IsRequired();

        builder.Property(r => r.BookingId).HasColumnName("booking_id").IsRequired();
        builder.Property(r => r.RequestedAmount).HasColumnName("requested_amount").HasPrecision(12, 2).IsRequired();
        builder.Property(r => r.CurrencyCode).HasColumnName("currency_code").HasMaxLength(3).IsRequired();

        builder.Property(r => r.Status).HasConversion<string>().HasColumnName("status").HasMaxLength(30).IsRequired();

        builder.Property(r => r.DecidedByUserId).HasColumnName("decided_by_user_id");
        builder.Property(r => r.DecidedAt).HasColumnName("decided_at");
        builder.Property(r => r.RejectionReason).HasColumnName("rejection_reason").HasMaxLength(500);
        builder.Property(r => r.ReceiptUrl).HasColumnName("receipt_url").HasMaxLength(500);

        builder.Property(r => r.CustomerEwalletProvider).HasColumnName("customer_ewallet_provider").HasMaxLength(50).IsRequired();
        builder.Property(r => r.CustomerEwalletNumber).HasColumnName("customer_ewallet_number").HasMaxLength(50).IsRequired();
        builder.Property(r => r.CustomerEwalletName).HasColumnName("customer_ewallet_name").HasMaxLength(200).IsRequired();

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").IsRequired();
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").IsRequired();

        builder.HasIndex(r => r.BookingId);
        builder.HasIndex(r => new { r.TenantId, r.Status });
    }
}
