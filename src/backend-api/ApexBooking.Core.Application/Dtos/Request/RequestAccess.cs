using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos.Request
{
    public sealed record RequestAccessDto(
    string BusinessName,
    BusinessType BusinessType,
    string Slug,
    string OwnerFirstName,
    string OwnerLastName,
    string OwnerEmail,
    SubscriptionPlanType RequestedPlan);

}
