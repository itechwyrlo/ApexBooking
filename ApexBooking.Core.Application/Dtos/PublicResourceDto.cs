namespace ApexBooking.Core.Application.Dtos
{
    public record PublicStaffDto(
        Guid StaffId,
        string Name,
        string? Description,
        List<StaffScheduleDto> AvailabilitySchedule,
        List<PublicServiceDto> ServicesOffered
    );
}