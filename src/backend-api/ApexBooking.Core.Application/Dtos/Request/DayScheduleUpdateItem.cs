namespace ApexBooking.Core.Application.Dtos.Request
{
    public record DayScheduleUpdateItem(
        DayOfWeek DayOfWeek,
        TimeOnly StartTime,
        TimeOnly EndTime,
        bool IsOff
    );
}