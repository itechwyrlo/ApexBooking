using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed class StaffAvailabilityDto
    {
        public Guid StaffId { get; init; }
        public List<DayScheduleDto> Schedules { get; init; } = new();
    }

    public sealed class DayScheduleDto
    {
        public int DayOfWeek { get; init; }
        public bool IsAvailable { get; init; }
        public string? StartTime { get; init; }
        public string? EndTime { get; init; }
        public List<BreakPeriodDto> Breaks { get; init; } = new();
    }

    public sealed class BreakPeriodDto
    {
        public string BreakStartTime { get; init; } = string.Empty;
        public string BreakEndTime { get; init; } = string.Empty;
        public string? Label { get; init; }
    }
}