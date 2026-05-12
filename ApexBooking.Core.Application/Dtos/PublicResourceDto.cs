namespace ApexBooking.Core.Application.Dtos
{
    public record PublicResourceDto(
        Guid ResourceId,
        string Name,
        string? Description,
        List<ResourceScheduleDto> AvailabilitySchedule,
        List<PublicServiceDto> ServicesOffered
    );
}