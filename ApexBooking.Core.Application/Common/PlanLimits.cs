using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Common;

public static class PlanLimits
{
    private const int BasicMaxResources = 3;
    private const int BasicMaxServices = 5;
    private const int BasicMaxBookingsPerMonth = 100;

    public static int? MaxResources(TenantPlan plan) => plan switch
    {
        TenantPlan.Basic => BasicMaxResources,
        TenantPlan.Professional => null,
        _ => BasicMaxResources
    };

    public static int? MaxServices(TenantPlan plan) => plan switch
    {
        TenantPlan.Basic => BasicMaxServices,
        TenantPlan.Professional => null,
        _ => BasicMaxServices
    };

    public static int? MaxBookingsPerMonth(TenantPlan plan) => plan switch
    {
        TenantPlan.Basic => BasicMaxBookingsPerMonth,
        TenantPlan.Professional => null,
        _ => BasicMaxBookingsPerMonth
    };
}
