using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;

namespace ApexBooking.Core.Domain.Services.BookingEngine
{
    public record SlotGenerationRequest(
        DateOnly TargetDate,
        int ServiceDurationMinutes,
        int ServiceBufferAfterMinutes,
        DayScheduleEntry BusinessOperatingHours,
        DayScheduleEntry StaffShiftHours,
        IReadOnlyCollection<StaffBreak> StaffBreaks,
        IReadOnlyCollection<BookingTimeSnapshot> ExistingBookings,
        int MinAdvanceBookingHours,
        // Already converted to the branch's local wall-clock time — see BranchTimeZoneConverter.
        DateTime NowBranchLocal,
        bool StaffIsOnApprovedTimeOff
    );

    public record BookingTimeSnapshot(TimeOnly StartTime, TimeOnly EndTime);

    // Distinguishes *why* a day came back with zero slots, so callers can surface a specific
    // reason to the customer instead of a generic "no open times." Order matters for
    // SlotGenerator's fail-fast checks (BranchClosed/StaffNotScheduled/StaffOnApprovedTimeOff are
    // checked before the timeline is even walked); TooSoonToBook and FullyBooked are only
    // distinguishable after walking it.
    public enum SlotUnavailabilityReason
    {
        None,
        BranchClosed,
        StaffNotScheduled,
        StaffOnApprovedTimeOff,
        TooSoonToBook,
        FullyBooked
    }

    public record SlotGenerationResult(IReadOnlyCollection<TimeOnly> Slots, SlotUnavailabilityReason Reason);
}