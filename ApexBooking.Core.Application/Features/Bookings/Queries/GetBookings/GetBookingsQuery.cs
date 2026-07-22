using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetBookings
{
    public sealed record GetBookingsQuery(QueryObjectParams param) : IQuery<PagedResult<TenantBookingsDto>>;
}