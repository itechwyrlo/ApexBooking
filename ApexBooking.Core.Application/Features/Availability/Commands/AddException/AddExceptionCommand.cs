using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Availability.Commands.AddException
{
    public sealed record AddExceptionCommand(
        Guid ResourceId,
        DateOnly ExceptionDate,
        ExceptionType ExceptionType,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        string? Note
    ) : ICommand<BaseResponse<bool>>;
}