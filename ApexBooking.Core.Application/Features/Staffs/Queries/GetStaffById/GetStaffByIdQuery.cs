using System;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffById
{
    public sealed record GetStaffByIdQuery(Guid StaffId) : IQuery<StaffDto>;
}