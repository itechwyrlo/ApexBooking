using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffExceptions
{
    public sealed record GetStaffQuery(QueryObjectParams param) 
    : IQuery<PagedResult<StaffDto>>;
}