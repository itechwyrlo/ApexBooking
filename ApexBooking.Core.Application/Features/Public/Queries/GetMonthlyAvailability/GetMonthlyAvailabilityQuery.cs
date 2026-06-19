using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetMonthlyAvailability
{
    public sealed record GetMonthlyAvailabilityQuery(
        string Slug,
        Guid ServiceId,
        int Year,
        int Month
    ) : IQuery<MonthlyAvailabilityDto>;
}
