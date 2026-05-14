using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.UpdateStaff
{
    public sealed record UpdateStaffCommand(
        Guid StaffId,
        string firstName,
        string lastName,
        string email,
        string contactNumber,
        string? Description,
        int Capacity
    ) : ICommand<BaseResponse<StaffId>>;
}