using System;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Availability.Commands.RemoveException
{
    public sealed record RemoveExceptionCommand(
        Guid StaffId,
        Guid ExceptionId
    ) : ICommand;
}