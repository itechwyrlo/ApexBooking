using System;
using System.Collections.Generic;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record MonthlyAvailabilityDto(
        int Year,
        int Month,
        List<DayAvailabilityDto> Days
    );

    public sealed record DayAvailabilityDto(
        DateOnly Date,
        bool IsAvailable
    );
}
