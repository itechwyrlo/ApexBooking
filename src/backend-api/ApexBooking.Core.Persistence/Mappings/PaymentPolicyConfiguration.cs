using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Mappings
{
    public class PaymentPolicyConfiguration : IEntityTypeConfiguration<PaymentPolicy>
    {
        public void Configure(EntityTypeBuilder<PaymentPolicy> builder)
        {
            builder.ToTable("payment_policy");

            builder.HasKey(p => p.PaymentPolicyId);

            builder.Property(p => p.PaymentPolicyId)
                .HasConversion(id => id.Value, v => new PaymentPolicyId(v))
                .ValueGeneratedNever()
                .HasColumnName("payment_policy_id");

            builder.Property(p => p.TenantId)
                .HasConversion(id => id.Value, v => new TenantId(v))
                .HasColumnName("tenant_id")
                .IsRequired();

            builder.Property(p => p.RequirementType)
                .HasConversion<string>()
                .HasColumnName("requirement_type")
                .IsRequired();

            builder.Property(p => p.DepositType)
                .HasConversion<string>()
                .HasColumnName("deposit_type")
                .IsRequired();

            builder.Property(p => p.DepositValue)
                .HasColumnName("deposit_value")
                .HasPrecision(12, 2)
                .IsRequired();

            builder.Property(p => p.OnTimeRefundPercent)
                .HasColumnName("on_time_refund_percent")
                .HasPrecision(5, 2)
                .HasDefaultValue(100m)
                .IsRequired();

            builder.Property(p => p.LateCancellationRefundPercent)
                .HasColumnName("late_cancellation_refund_percent")
                .HasPrecision(5, 2)
                .HasDefaultValue(0m)
                .IsRequired();

            builder.Property(p => p.RefundEnabled)
                .HasColumnName("refund_enabled")
                .HasDefaultValue(false)
                .IsRequired();

            builder.Property(p => p.RefundReviewDeadlineDays)
                .HasColumnName("refund_review_deadline_days")
                .HasDefaultValue(7)
                .IsRequired();

            builder.Property(p => p.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(p => p.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.HasIndex(p => p.TenantId).IsUnique();
        }
    }
}
