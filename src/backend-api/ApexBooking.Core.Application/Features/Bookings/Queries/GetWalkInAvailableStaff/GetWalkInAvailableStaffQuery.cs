using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetWalkInAvailableStaff
{
    public record GetWalkInAvailableStaffQuery(Guid BranchId, Guid ServiceId) : IQuery<IReadOnlyCollection<WalkInStaffAvailability>>;
}
