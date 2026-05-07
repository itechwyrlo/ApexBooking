using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.WebApi.Dtos
{
    public record CreateBookingRequestDto(
        Guid ServiceId,
        Guid ResourceId,
        DateOnly ScheduledDate,
        TimeOnly ScheduledStartTime,
        string? CustomerNotes
    );
}