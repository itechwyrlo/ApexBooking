using System;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class PaymentPolicy : ITenantEntity
{
    public PaymentPolicyId PaymentPolicyId { get; protected set; } = default!;
    public TenantId TenantId { get; protected set; } = default!;
    public PaymentRequirementType RequirementType { get; private set; }
    public DepositType DepositType { get; private set; }
    public decimal DepositValue { get; private set; }
    // How much of the payment is refunded for an ON-TIME cancellation. Defaults to 100 — a
    // business opts INTO keeping an admin fee by lowering this, rather than opting into being
    // generous. Deliberately separate from LateCancellationRefundPercent below: on-time refund
    // should never be lower than the late-cancellation rate, but nothing enforces that ordering
    // — it's a tenant configuration choice, not a domain invariant.
    public decimal OnTimeRefundPercent { get; private set; }
    // How much of the payment is refunded for a LATE cancellation when LateCancellationPolicy is
    // PartialRefund. Defaults to 0 — a business opts INTO being more generous on a late
    // cancellation by raising this, matching that late cancellations hurt the business more and
    // should be penalized harder than on-time ones by default.
    public decimal LateCancellationRefundPercent { get; private set; }
    // Whether this tenant does refunds at all. Defaults false — a fresh tenant, and every tenant
    // that existed before this flag shipped, starts with refunds off. A cancelled online-paid
    // booking produces no RefundRequest, no Pending status, no refund UI, until a tenant
    // explicitly turns this on in Payment Settings.
    public bool RefundEnabled { get; private set; }
    // How many days staff have to act on a PendingReview RefundRequest before it's flagged
    // overdue in the UI. Purely a reminder threshold — nothing server-side auto-acts when it
    // passes.
    public int RefundReviewDeadlineDays { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }
    public Tenant Tenant { get; private set; } = default!;

    protected PaymentPolicy() { }

    public PaymentPolicy(TenantId tenantId)
    {
        PaymentPolicyId = new PaymentPolicyId(Guid.NewGuid());
        TenantId = tenantId;
        RequirementType = PaymentRequirementType.None;
        DepositType = DepositType.Percentage;
        DepositValue = 0m;
        OnTimeRefundPercent = 100m;
        LateCancellationRefundPercent = 0m;
        RefundEnabled = false;
        RefundReviewDeadlineDays = 7;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    internal static PaymentPolicy CreateDefault(TenantId tenantId)
    {
        return new PaymentPolicy(tenantId);
    }

    public void UpdatePolicy(
        PaymentRequirementType requirementType,
        DepositType depositType,
        decimal depositValue,
        decimal onTimeRefundPercent,
        decimal lateCancellationRefundPercent,
        int refundReviewDeadlineDays,
        bool refundEnabled)
    {
        if (requirementType == PaymentRequirementType.None)
        {
            depositValue = 0m;
        }
        else
        {
            if (depositValue < 0)
                throw new BusinessRuleBrokenException("Deposit value cannot be a negative amount.");

            if (depositType == DepositType.Percentage && depositValue > 100)
                throw new BusinessRuleBrokenException("A percentage-based deposit requirement cannot exceed 100%.");
        }

        if (onTimeRefundPercent < 0 || onTimeRefundPercent > 100)
            throw new BusinessRuleBrokenException("On-time refund percentage must sit between 0% and 100%.");

        if (lateCancellationRefundPercent < 0 || lateCancellationRefundPercent > 100)
            throw new BusinessRuleBrokenException("Late cancellation refund percentage must sit between 0% and 100%.");

        if (refundReviewDeadlineDays < 1)
            throw new BusinessRuleBrokenException("Refund review deadline must be at least 1 day.");

        RequirementType = requirementType;
        DepositType = depositType;
        DepositValue = depositValue;
        OnTimeRefundPercent = onTimeRefundPercent;
        LateCancellationRefundPercent = lateCancellationRefundPercent;
        RefundReviewDeadlineDays = refundReviewDeadlineDays;
        RefundEnabled = refundEnabled;
        UpdatedAt = DateTime.UtcNow;
    }
}
