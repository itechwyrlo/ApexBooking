using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Services.Notification;

namespace ApexBooking.Infrastructure.ExternalServices.Plan;

/// <summary>
/// Per-plan limits (ADR-053). MVP SMS quotas (08 §6): Basic 100 / Professional 500 / Enterprise a
/// high fixed default (a per-tenant override home is deferred until Enterprise ships).
/// </summary>
public sealed class PlanPolicy : IPlanPolicy
{
    public int GetSmsMonthlyLimit(SubscriptionPlanType plan) => plan switch
    {
        SubscriptionPlanType.Basic => 100,
        SubscriptionPlanType.Professional => 500,
        SubscriptionPlanType.Enterprise => 100_000,
        _ => 0
    };
}
