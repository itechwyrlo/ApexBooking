using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetReassignableStaff
{
    public record GetReassignableStaffQuery(Guid BookingId) : IQuery<IReadOnlyCollection<ReassignableStaffDto>>;
}
