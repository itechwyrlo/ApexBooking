namespace ApexBooking.Core.Application.Dtos
{
    public record StaffScheduleDto(
        string DayOfWeek,
        string StartTime,
        string EndTime
    );
}
