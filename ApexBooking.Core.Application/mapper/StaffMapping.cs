using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Services.Mappings;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.Resources.Mappings;

public static class ResourceMappings
{
    public static StaffAvailabilityExceptionDto ToExceptionDto(
        this StaffAvailabilityException exception) => new(
            exception.StaffAvailabilityExceptionId.Value,
            exception.ExceptionDate,
            exception.ExceptionType.ToString(),
            exception.StartTime,
            exception.EndTime,
            exception.Note
        );

    public static StaffDto ToStaffDto(this Staff staff) => new(
        staff.StaffId.Value,
        staff.FirstName,
        staff.LastName,
        staff.Email,
        staff.ContactNumber,
        staff.Description,
        staff.Capacity,
        staff.IsActive,
        staff.CreatedAt,
        staff.UpdatedAt
    );

    public static PublicStaffDto ToPublicResourceDto(
        this Staff resource,
        IEnumerable<Service> services) => new(
        StaffId: resource.StaffId.Value,
        Name: resource.FirstName + ' ' + resource.LastName,
        Description: resource.Description,
        AvailabilitySchedule: resource.AvailabilitySchedules
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.DayOfWeek)
            .Select(s => new StaffScheduleDto(
                s.DayOfWeek.ToString(),
                s.StartTime!.Value.ToString("HH:mm"),
                s.EndTime!.Value.ToString("HH:mm")))
            .ToList(),
        ServicesOffered: services
            .Select(s => s.ToPublicServiceDto())
            .ToList());

    public static StaffAvailabilityDto ToAvailabilityDto(this Staff resource) => new()
    {
        StaffId = resource.StaffId.Value,
        Schedules = resource.AvailabilitySchedules
            .OrderBy(s => s.DayOfWeek)
            .Select(s => s.ToDayScheduleDto())
            .ToList()
    };

    public static DayScheduleDto ToDayScheduleDto(this StaffAvailabilitySchedule schedule) => new()
    {
        DayOfWeek = (int)schedule.DayOfWeek,
        IsAvailable = schedule.IsAvailable,
        StartTime = schedule.StartTime?.ToString("HH:mm"),
        EndTime = schedule.EndTime?.ToString("HH:mm"),
        Breaks = schedule.BreakPeriods
            .Select(b => new BreakPeriodDto
            {
                BreakStartTime = b.BreakStartTime.ToString("HH:mm"),
                BreakEndTime = b.BreakEndTime.ToString("HH:mm"),
                Label = b.Label
            })
            .ToList()
    };
}
