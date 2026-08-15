using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos.Request
{
    public record OperatingHoursUpdateItem(
        DayOfWeek DayOfWeek, 
        TimeOnly StartTime, 
        TimeOnly EndTime, 
        bool IsOff
    );
}