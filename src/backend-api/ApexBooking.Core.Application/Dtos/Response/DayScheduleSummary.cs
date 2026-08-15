using System;

namespace ApexBooking.Core.Application.Dtos.Response
{
    public record DayScheduleSummary(
        DayOfWeek DayOfWeek,
        TimeOnly StartTime,
        TimeOnly EndTime,
        bool IsOff
    );
}
