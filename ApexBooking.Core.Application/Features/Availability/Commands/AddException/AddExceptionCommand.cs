using System;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.Features.Availability.Commands.AddException
{
    public sealed record AddExceptionCommand(
        Guid ResourceId,
        DateOnly ExceptionDate,
        ExceptionType ExceptionType,
        TimeOnly? StartTime,
        TimeOnly? EndTime,
        string? Note
    ) : ICommand;
}