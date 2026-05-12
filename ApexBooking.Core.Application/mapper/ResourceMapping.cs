using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Services.Mappings;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.Resources.Mappings;

public static class ResourceMappings
{
    public static ResourceAvailabilityExceptionDto ToExceptionDto(
        this ResourceAvailabilityException exception) => new(
            exception.ResourceAvailabilityExceptionId.Value,
            exception.ExceptionDate,
            exception.ExceptionType.ToString(),
            exception.StartTime,
            exception.EndTime,
            exception.Note
        );

    public static ResourceDto ToResourceDto(this Resource resource) => new(
        resource.ResourceId.Value,
        resource.Name,
        resource.Description,
        resource.ResourceType.ToString(),
        resource.Capacity,
        resource.IsActive,
        resource.CreatedAt,
        resource.UpdatedAt
    );

    public static PublicResourceDto ToPublicResourceDto(
        this Resource resource,
        IEnumerable<Service> services) => new(
        ResourceId: resource.ResourceId.Value,
        Name: resource.Name,
        Description: resource.Description,
        AvailabilitySchedule: resource.AvailabilitySchedules
            .Where(s => s.IsAvailable)
            .OrderBy(s => s.DayOfWeek)
            .Select(s => new ResourceScheduleDto(
                s.DayOfWeek.ToString(),
                s.StartTime!.Value.ToString("HH:mm"),
                s.EndTime!.Value.ToString("HH:mm")))
            .ToList(),
        ServicesOffered: services
            .Select(s => s.ToPublicServiceDto())
            .ToList());

    public static ResourceAvailabilityDto ToAvailabilityDto(this Resource resource) => new()
    {
        ResourceId = resource.ResourceId.Value,
        Schedules = resource.AvailabilitySchedules
            .OrderBy(s => s.DayOfWeek)
            .Select(s => s.ToDayScheduleDto())
            .ToList()
    };

    public static DayScheduleDto ToDayScheduleDto(this ResourceAvailabilitySchedule schedule) => new()
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
