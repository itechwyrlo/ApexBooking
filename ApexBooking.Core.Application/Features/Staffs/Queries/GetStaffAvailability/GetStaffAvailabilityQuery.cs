using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Staffs.Queries.GetStaffAvailability
{
    public sealed record GetStaffAvailabilityQuery(Guid StaffId)
        : IQuery<BaseResponse<StaffAvailabilityDto>>;
}