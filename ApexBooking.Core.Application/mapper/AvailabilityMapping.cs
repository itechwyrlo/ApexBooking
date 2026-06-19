using System;
using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos;

namespace ApexBooking.Core.Application.Resources.Mappings
{
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
                slots);
        }
    }
}
