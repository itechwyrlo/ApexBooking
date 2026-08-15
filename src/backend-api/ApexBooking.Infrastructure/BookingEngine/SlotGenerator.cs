using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Services.BookingEngine;

namespace ApexBooking.Infrastructure.BookingEngine
{
    public class SlotGenerator : ISlotGenerator
    {
        // The scheduling grid step interval increment (e.g., slots step forward every 15 minutes)
        private const int TimeStepMinutes = 15;

        public SlotGenerationResult GenerateAvailableSlots(SlotGenerationRequest request)
        {
            var availableSlots = new List<TimeOnly>();

            // 1. Fail-fast if the business or the specific staff member is off on this day
            if (request.BusinessOperatingHours.IsOff)
                return new SlotGenerationResult(availableSlots, SlotUnavailabilityReason.BranchClosed);

            if (request.StaffShiftHours.IsOff)
                return new SlotGenerationResult(availableSlots, SlotUnavailabilityReason.StaffNotScheduled);

            if (request.StaffIsOnApprovedTimeOff)
                return new SlotGenerationResult(availableSlots, SlotUnavailabilityReason.StaffOnApprovedTimeOff);

            // Earliest branch-local moment this service can be booked for, given the tenant's
            // (or this service's override) minimum-advance-booking policy.
            var earliestBookableMoment = request.NowBranchLocal.AddHours(request.MinAdvanceBookingHours);

            // 2. Intersect store hours and staff shift hours to establish the actual working timeline window
            TimeOnly workingStartTime = request.StaffShiftHours.StartTime > request.BusinessOperatingHours.StartTime
                ? request.StaffShiftHours.StartTime
                : request.BusinessOperatingHours.StartTime;

            TimeOnly workingEndTime = request.StaffShiftHours.EndTime < request.BusinessOperatingHours.EndTime
                ? request.StaffShiftHours.EndTime
                : request.BusinessOperatingHours.EndTime;

            // Calculate the absolute total blocked length required for this service session
            int totalNeededMinutes = request.ServiceDurationMinutes + request.ServiceBufferAfterMinutes;

            // 3. Slide the timeline pointer through the active workday
            TimeOnly currentPointer = workingStartTime;

            // Tracks whether at least one candidate slot ever cleared the advance-notice check —
            // lets the empty-result path below tell "everything was too soon" apart from
            // "some times were bookable but all collided with breaks/existing appointments."
            bool sawAnyTimelyCandidate = false;

            while (currentPointer.AddMinutes(totalNeededMinutes) <= workingEndTime)
            {
                TimeOnly slotStartTime = currentPointer;
                TimeOnly slotEndTime = currentPointer.AddMinutes(request.ServiceDurationMinutes);
                TimeOnly totalBlockEndTime = currentPointer.AddMinutes(totalNeededMinutes);

                // 4. Verification Check A: Does this window collide with any configured staff breaks?
                bool collidesWithBreak = request.StaffBreaks.Any(b =>
                    b.StartTime < totalBlockEndTime && b.EndTime > slotStartTime);

                // 5. Verification Check B: Does this window overlap with any existing appointments?
                bool collidesWithBooking = request.ExistingBookings.Any(b =>
                    b.StartTime < totalBlockEndTime && b.EndTime > slotStartTime);

                // 6. Verification Check C: Is this slot inside the minimum-advance-booking lead-time window?
                var slotMoment = request.TargetDate.ToDateTime(slotStartTime);
                bool tooSoonToBook = slotMoment < earliestBookableMoment;

                if (!tooSoonToBook)
                {
                    sawAnyTimelyCandidate = true;

                    // 7. If every timeline is completely clear, register this start time as an open slot!
                    if (!collidesWithBreak && !collidesWithBooking)
                    {
                        availableSlots.Add(slotStartTime);
                    }
                }

                // Move the pointer forward by the structural configuration step interval
                currentPointer = currentPointer.AddMinutes(TimeStepMinutes);

                // Defensive break guard to prevent infinite looping at midnight boundaries
                if (currentPointer == TimeOnly.MinValue)
                    break;
            }

            if (availableSlots.Count > 0)
                return new SlotGenerationResult(availableSlots, SlotUnavailabilityReason.None);

            var reason = sawAnyTimelyCandidate ? SlotUnavailabilityReason.FullyBooked : SlotUnavailabilityReason.TooSoonToBook;
            return new SlotGenerationResult(availableSlots, reason);
        }
    }
}
