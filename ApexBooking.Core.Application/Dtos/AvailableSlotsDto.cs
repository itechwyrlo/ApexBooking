using System;
using System.Collections.Generic;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record AvailableSlotsDto(
        Guid ServiceId,
        Guid? StaffId,
        DateOnly Date,
        int DurationMinutes,
        IReadOnlyList<string> AvailableSlots
    );
}