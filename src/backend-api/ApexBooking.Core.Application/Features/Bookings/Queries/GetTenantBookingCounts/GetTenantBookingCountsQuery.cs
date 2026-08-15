using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantBookingCounts
{
    public record GetTenantBookingCountsQuery(DateOnly Date) : IQuery<TenantBookingCountsDto>;
}
