using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record AvailableSlotsDto(
    Guid ServiceId,
    Guid? StaffId,
    DateOnly Date,
    int DurationMinutes,
    IReadOnlyList<string> AvailableSlots
);
    public static class AvailabilityMappings
    {
        public static AvailableSlotsDto ToAvailableSlotsDto(
            Guid serviceId,
            Guid? resourceId,
            DateOnly date,
            int durationMinutes,
            IReadOnlyList<string> slots)
        {
            return new AvailableSlotsDto(
                serviceId,
                resourceId,
                date,
                durationMinutes,
                slots
            );
        }
    }
}