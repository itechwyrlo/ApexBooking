using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Availability.Commands.SetResourceAvailability
{
    public record SetResourceAvailabilityCommand(
        Guid StaffId,
        List<DayScheduleDto> Schedules
    ) : ICommand;
}