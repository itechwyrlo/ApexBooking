using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Availability.Commands.RemoveException
{
    public sealed record RemoveExceptionCommand(
        Guid StaffId,
        Guid ExceptionId
    ) : ICommand<BaseResponse<bool>>;
}