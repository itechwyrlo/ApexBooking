namespace ApexBooking.Core.Application.Dtos
{
    public record ResourceScheduleDto(
        string DayOfWeek,
        string StartTime,
        string EndTime
    );
}
