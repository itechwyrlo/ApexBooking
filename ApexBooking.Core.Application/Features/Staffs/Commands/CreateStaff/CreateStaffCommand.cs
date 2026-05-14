using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Staffs.Commands.CreateStaff
{
    public record CreateStaffCommand(
        string FirstName,
        string LastName,
        string email,
        string contactNumber,
        int Capacity,
        string? Description
    ) : ICommand<BaseResponse<Staff>>;
}