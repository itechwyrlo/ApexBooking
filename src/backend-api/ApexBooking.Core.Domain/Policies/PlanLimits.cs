using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Domain.Policies;

/// <summary>
/// Code-level plan catalog (ADR-053): plan limits are pure rules with no persistence.
/// Resolved from a tenant's <see cref="SubscriptionPlanType"/>. A null limit means unlimited.
/// </summary>
public static class PlanLimits
{
    private const int BasicMaxResources = 3;
    private const int BasicMaxServices = 5;
    private const int BasicMaxBookingsPerMonth = 100;

    private const int ProfessionalMaxResources = 15;
    private const int ProfessionalMaxServices = 50;

    public static int? MaxResources(SubscriptionPlanType plan) => plan switch
    {
        SubscriptionPlanType.Basic => BasicMaxResources,
        SubscriptionPlanType.Professional => ProfessionalMaxResources,
        SubscriptionPlanType.Enterprise => null,
        _ => BasicMaxResources
    };

    public static int? MaxServices(SubscriptionPlanType plan) => plan switch
    {
        SubscriptionPlanType.Basic => BasicMaxServices,
        SubscriptionPlanType.Professional => ProfessionalMaxServices,
        SubscriptionPlanType.Enterprise => null,
        _ => BasicMaxServices
    };

    public static int? MaxBookingsPerMonth(SubscriptionPlanType plan) => plan switch
    {
        SubscriptionPlanType.Basic => BasicMaxBookingsPerMonth,
        SubscriptionPlanType.Professional => null,
        SubscriptionPlanType.Enterprise => null,
        _ => BasicMaxBookingsPerMonth
    };
}
