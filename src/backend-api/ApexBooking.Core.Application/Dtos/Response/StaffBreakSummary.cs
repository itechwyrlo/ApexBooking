using System;

namespace ApexBooking.Core.Application.Dtos.Response
{
    public record StaffBreakSummary(
        Guid Id,
        string Name,
        TimeOnly StartTime,
        TimeOnly EndTime
    );
}
