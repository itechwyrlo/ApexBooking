using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.ValueObjects;


public record DayScheduleEntry
{
    public DayOfWeek DayOfWeek { get; }
    public TimeOnly StartTime { get; }
    public TimeOnly EndTime { get; }
    public bool IsOff { get; }

    private DayScheduleEntry() { }

    public DayScheduleEntry(DayOfWeek dayOfWeek, TimeOnly startTime, TimeOnly endTime, bool isOff)
    {
        if (!isOff && startTime >= endTime)
            throw new BusinessRuleBrokenException($"Working hours for {dayOfWeek}: start time must be earlier than end time.");

        DayOfWeek = dayOfWeek;
        StartTime = startTime;
        EndTime = endTime;
        IsOff = isOff;
    }

    public static DayScheduleEntry OffFor(DayOfWeek day) => new(day, TimeOnly.MinValue, TimeOnly.MinValue, isOff: true);
}

