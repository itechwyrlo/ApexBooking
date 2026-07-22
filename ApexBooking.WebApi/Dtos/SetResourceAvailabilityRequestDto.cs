using ApexBooking.Core.Application.Dtos;

namespace ApexBooking.WebApi.Dtos
{
    public class SetResourceAvailabilityRequestDto
    {
        public List<DayScheduleDto> Schedules { get; set; } = new();
    }
}