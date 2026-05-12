using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class TenantPaymentPolicy
{
    public TenantPaymentPolicyId TenantPaymentPolicyId { get; protected set; } = default!;
    public TenantId TenantId { get; protected set; } = default!;

    /// <summary>Whether online payment must be collected to confirm a booking.</summary>
    public bool PaymentRequired { get; private set; }

    /// <summary>When PaymentRequired, charge only a deposit instead of the full amount.</summary>
    public bool DepositOnly { get; private set; }

    /// <summary>Whether the deposit is expressed as a percentage or a fixed amount.</summary>
    public DepositType DepositType { get; private set; }

    /// <summary>Deposit percentage (0-100) or fixed monetary amount, depending on DepositType.</summary>
    public decimal DepositValue { get; private set; }

    /// <summary>Percentage refunded when LateCancellationPolicy = PartialRefund on TenantSettings.</summary>
    public decimal RefundPercent { get; private set; }

    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    public Tenant Tenant { get; private set; } = default!;

    protected TenantPaymentPolicy() { }

    [SetsRequiredMembers]
    public TenantPaymentPolicy(TenantId tenantId)
    {
        TenantPaymentPolicyId = new TenantPaymentPolicyId(Guid.NewGuid());
        TenantId = tenantId;
        PaymentRequired = false;
        DepositOnly = false;
        DepositType = DepositType.Percentage;
        DepositValue = 0m;
        RefundPercent = 0m;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static TenantPaymentPolicy Create(TenantId tenantId) => new(tenantId);

    public void UpdatePolicy(
        bool? paymentRequired = null,
        bool? depositOnly = null,
        DepositType? depositType = null,
        decimal? depositValue = null,
        decimal? refundPercent = null)
    {
        if (paymentRequired.HasValue)
            PaymentRequired = paymentRequired.Value;

        if (depositOnly.HasValue)
            DepositOnly = depositOnly.Value;

        if (depositType.HasValue)
            DepositType = depositType.Value;

        if (depositValue.HasValue && depositValue.Value >= 0)
            DepositValue = depositValue.Value;

        if (refundPercent.HasValue && refundPercent.Value >= 0 && refundPercent.Value <= 100)
            RefundPercent = refundPercent.Value;

        UpdatedAt = DateTime.UtcNow;
    }
}

public enum DepositType
{
    Percentage,
    FixedAmount
}
