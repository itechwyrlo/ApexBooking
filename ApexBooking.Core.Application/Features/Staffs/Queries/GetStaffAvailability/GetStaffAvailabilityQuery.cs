using System;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffAvailability
{
    public sealed record GetStaffAvailabilityQuery(Guid StaffId)
        : IQuery<StaffAvailabilityDto>;
}