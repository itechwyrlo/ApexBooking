using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;
public class TenantSubscription
{
    public TenantSubscriptionId Id { get; private set; } = default!;
    public TenantId TenantId { get; private set; } = default!;
    public SubscriptionPlanType PlanType { get; private set; }
    public TenantSubscriptionStatus Status { get; private set; }
    public DateTime? CurrentPeriodEnd { get; private set; }
    public string? ProviderSubscriptionReference { get; private set; }
    public string? ProviderCustomerReference { get; private set; }
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    protected TenantSubscription() { }

    private TenantSubscription(TenantId tenantId, SubscriptionPlanType planType)
    {
        Id = new TenantSubscriptionId(Guid.NewGuid());
        TenantId = tenantId;
        PlanType = planType;
        Status = TenantSubscriptionStatus.Trialing;
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static TenantSubscription StartTrial(TenantId tenantId, SubscriptionPlanType planType)
    {
        if (tenantId is null) throw new BusinessRuleBrokenException("Tenant is required.");
        return new TenantSubscription(tenantId, planType);
    }

    public void Activate(DateTime currentPeriodEnd, string? providerSubscriptionReference = null, string? providerCustomerReference = null)
    {
        Status = TenantSubscriptionStatus.Active;
        CurrentPeriodEnd = currentPeriodEnd;
        if (providerSubscriptionReference is not null) ProviderSubscriptionReference = providerSubscriptionReference;
        if (providerCustomerReference is not null) ProviderCustomerReference = providerCustomerReference;
        Touch();
    }

    public void MarkPastDue() => Transition(TenantSubscriptionStatus.PastDue);
    public void Cancel() => Transition(TenantSubscriptionStatus.Canceled);

    public void ChangePlan(SubscriptionPlanType newPlan)
    {
        PlanType = newPlan;
        Touch();
    }

    private void Transition(TenantSubscriptionStatus next)
    {
        Status = next;
        Touch();
    }

    private void Touch() => UpdatedAt = DateTime.UtcNow;
}
