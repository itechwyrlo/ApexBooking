using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.BookingEngine;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.Slots
{
    public class GetAvailableSlotsHandler : IQueryHandler<GetAvailableSlotsQuery, AvailableSlotsResult>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ISlotGenerator _slotGenerator;
        private readonly ITenantEntity _tenantEntity;

        public GetAvailableSlotsHandler(IUnitOfWork unitOfWork, ISlotGenerator slotGenerator, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _slotGenerator = slotGenerator;
            _tenantEntity = tenantEntity;
        }

        public async Task<AvailableSlotsResult> Handle(
            GetAvailableSlotsQuery query,
            CancellationToken cancellationToken)
        {
            // 1. Single Database Trip: Load the isolated tenant aggregate along with every structure this calculation needs.
            // Tenant does not implement ITenantEntity, so the global EF query filter doesn't cover this
            // lookup — scope explicitly via the ambient ITenantEntity.
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to calculate slots. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: [t => t.Branches, t => t.Members, t => t.Services, t => t.Bookings, t => t.BookingPolicy!]);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Failed to calculate slots. Workspace environment could not be verified.");

            var branch = tenant.Branches.FirstOrDefault(b => b.BranchId.Value == query.BranchId && b.IsActive)
                ?? throw new BusinessRuleBrokenException("The selected branch is unavailable.");

            var staff = tenant.Members.FirstOrDefault(m => m.TenantMemberId.Value == query.StaffId);
            if (staff is null || !staff.IsActive)
                throw new BusinessRuleBrokenException("The requested staff member is not available.");

            // 2. Branch isolation: the staff member must be actively deployed to the chosen branch
            if (staff.BranchId != branch.BranchId)
                throw new BusinessRuleBrokenException("The selected staff member is not deployed to this branch.");

            var service = tenant.Services.FirstOrDefault(s => s.ServiceId.Value == query.ServiceId);
            if (service is null || !service.IsActive)
                throw new BusinessRuleBrokenException("The requested service catalog item is missing or deactivated.");

            // 3. Resolve day-of-week variables to extract baseline shift maps
            DayOfWeek targetDay = query.TargetDate.DayOfWeek;

            // Branch operating hours replace the old tenant-wide business hours
            var businessHours = branch.OperatingHours
                .FirstOrDefault(h => h.DayOfWeek == targetDay) ?? DayScheduleEntry.OffFor(targetDay);

            var staffHours = staff.WeeklySchedule
                .FirstOrDefault(s => s.DayOfWeek == targetDay) ?? DayScheduleEntry.OffFor(targetDay);

            // 4. Existing active booked blocks, scoped to this branch as well as this staff member
            var dailyBookings = tenant.Bookings
                .Where(b => b.BranchId == branch.BranchId
                         && b.StaffId == staff.TenantMemberId
                         && b.ScheduledDate == query.TargetDate
                         && b.Status == BookingStatus.Scheduled)
                .Select(b => new BookingTimeSnapshot(b.ScheduledStartTime, b.ScheduledEndTime))
                .ToList();

            // 4b. Effective minimum-advance-booking window: this service's own override, if configured,
            // otherwise the tenant-wide BookingPolicy default.
            int minAdvanceBookingHours = service.MinAdvanceBookingHoursOverride
                ?? tenant.BookingPolicy?.MinAdvanceBookingHours
                ?? 0;

            var nowBranchLocal = BranchTimeZoneConverter.ToBranchLocalTime(DateTime.UtcNow, branch.TimeZoneId);

            // 5. Build the immutable request bundle object
            var requestPayload = new SlotGenerationRequest(
                TargetDate: query.TargetDate,
                ServiceDurationMinutes: service.DurationMinutes,
                ServiceBufferAfterMinutes: service.BufferAfterMinutes,
                BusinessOperatingHours: businessHours,
                StaffShiftHours: staffHours,
                StaffBreaks: staff.Breaks,
                ExistingBookings: dailyBookings,
                MinAdvanceBookingHours: minAdvanceBookingHours,
                NowBranchLocal: nowBranchLocal,
                StaffIsOnApprovedTimeOff: staff.HasApprovedTimeOff(query.TargetDate)
            );

            // 6. Invoke the stateless, pure in-memory calculation algorithm
            var generationResult = _slotGenerator.GenerateAvailableSlots(requestPayload);

            // 7. Project raw times cleanly into the user interface summary design models
            var slotResponses = generationResult.Slots.Select(time => new AvailableSlotResponse(
                TimeString: DateTime.Today.Add(time.ToTimeSpan()).ToString("hh:mm tt"), // e.g., renders as "02:30 PM"
                RawTime: time
            )).ToList();

            var unavailableReason = BuildUnavailableReason(
                generationResult.Reason, branch, staff, targetDay, minAdvanceBookingHours,
                query.TargetDate, nowBranchLocal, businessHours, staffHours);

            return new AvailableSlotsResult(slotResponses, unavailableReason);
        }

        // Only the handler knows the human-facing names (branch, staff) and policy numbers needed
        // to phrase this — SlotGenerator stays a pure algorithm reporting a structured reason code.
        private static string? BuildUnavailableReason(
            SlotUnavailabilityReason reason,
            Branch branch,
            TenantMember staff,
            DayOfWeek targetDay,
            int minAdvanceBookingHours,
            DateOnly targetDate,
            DateTime nowBranchLocal,
            DayScheduleEntry businessHours,
            DayScheduleEntry staffHours)
        {
            if (reason != SlotUnavailabilityReason.TooSoonToBook)
            {
                return reason switch
                {
                    SlotUnavailabilityReason.None => null,
                    SlotUnavailabilityReason.BranchClosed => $"{branch.BranchName} is closed on {targetDay}s.",
                    SlotUnavailabilityReason.StaffNotScheduled => $"{staff.FirstName} doesn't work on {targetDay}s.",
                    SlotUnavailabilityReason.StaffOnApprovedTimeOff => $"{staff.FirstName} is on approved time off this day.",
                    SlotUnavailabilityReason.FullyBooked => "All appointments are booked for this day. Try another date.",
                    _ => null
                };
            }

            // TooSoonToBook is ambiguous on its own: zero slots today can mean either "it's
            // genuinely still too early relative to the advance-notice policy" or "the wall clock
            // has simply moved past today's working window already." A customer picking today's
            // date deserves to know which — otherwise a 0-hour policy reads as "book anytime" while
            // the page shows nothing. Only today's date carries this ambiguity: a future date with
            // no slots is unambiguously the advance-notice policy at work.
            bool isTargetToday = targetDate == DateOnly.FromDateTime(nowBranchLocal);
            if (!isTargetToday)
                return $"This service requires at least {minAdvanceBookingHours} hour(s) advance notice.";

            TimeOnly effectiveStart = staffHours.StartTime > businessHours.StartTime ? staffHours.StartTime : businessHours.StartTime;
            TimeOnly effectiveEnd = staffHours.EndTime < businessHours.EndTime ? staffHours.EndTime : businessHours.EndTime;
            string window = $"{DateTime.Today.Add(effectiveStart.ToTimeSpan()):h:mm tt}–{DateTime.Today.Add(effectiveEnd.ToTimeSpan()):h:mm tt}";
            string nowText = $"{DateTime.Today.Add(nowBranchLocal.TimeOfDay):h:mm tt}";

            return minAdvanceBookingHours > 0
                ? $"It's currently {nowText}. {staff.FirstName} is available {window} today, and this service needs at least {minAdvanceBookingHours} hour(s) advance notice — no time remains today. Please choose another date."
                : $"It's currently {nowText}, and today's booking window ({window}) has already ended. Please choose another date.";
        }
    }
}
