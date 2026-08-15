using System;
using System.Collections.Generic;

namespace ApexBooking.Core.Application.Dtos.Response
{
    // One eligible staff member's real-time picture for the walk-in flow — recommended start time
    // for right now, how long that opening lasts, and the rest of today's open times so the caller
    // can switch time without switching staff. Every field is pre-computed server-side from the
    // same ISlotGenerator used by the public wizard, so the frontend never re-derives availability.
    public record WalkInStaffAvailability(
        Guid TenantMemberId,
        string FullName,
        string? CustomJobTitle,
        string? PhotoUrl,
        bool IsAvailableNow,
        string? RecommendedTimeDisplay,
        TimeOnly? RecommendedTimeRaw,
        string? AvailableUntilDisplay,
        IReadOnlyCollection<WalkInTimeOption> AlternateTimes,
        // Populated only when RecommendedTimeRaw is null — e.g. "On Break", "Off Today", "On Approved Time Off", "Fully Booked".
        string? UnavailableReason
    );

    public record WalkInTimeOption(string Display, TimeOnly Raw);
}
