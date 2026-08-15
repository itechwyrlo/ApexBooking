using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.BookingEngine;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetWalkInAvailableStaff
{
    public class GetWalkInAvailableStaffHandler : IQueryHandler<GetWalkInAvailableStaffQuery, IReadOnlyCollection<WalkInStaffAvailability>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;
        private readonly IUserContextService _userContext;
        private readonly ISlotGenerator _slotGenerator;

        public GetWalkInAvailableStaffHandler(
            IUnitOfWork unitOfWork,
            ITenantEntity tenantEntity,
            IUserContextService userContext,
            ISlotGenerator slotGenerator)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
            _userContext = userContext;
            _slotGenerator = slotGenerator;
        }

        public async Task<IReadOnlyCollection<WalkInStaffAvailability>> Handle(GetWalkInAvailableStaffQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Workspace scope could not be verified.");

            var tenant = await _unitOfWork.TenantRepository.GetForWalkInAvailabilityAsync(tenantId, cancellationToken);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Workspace scope could not be verified.");

            var branch = tenant.Branches.FirstOrDefault(b => b.BranchId.Value == query.BranchId && b.IsActive)
                ?? throw new BusinessRuleBrokenException("The selected branch is unavailable.");

            // A plain Staff member may only look up walk-in availability for their own assigned
            // branch — Owner/Admin manage the whole tenant and may pick any of its branches.
            var currentUserId = _userContext.GetCurrentUserId();
            var callerMember = tenant.Members.FirstOrDefault(m => m.UserId == currentUserId && m.IsActive);
            if (callerMember is { Role: SystemRole.Staff } && callerMember.BranchId != branch.BranchId)
                throw new BusinessRuleBrokenException("You can only look up walk-in availability for your own assigned branch.");

            var targetService = tenant.Services.FirstOrDefault(s => s.ServiceId.Value == query.ServiceId);
            if (targetService is null || !targetService.IsActive)
                throw new BusinessRuleBrokenException("The requested service catalog item is unavailable.");

            var nowBranchLocal = BranchTimeZoneConverter.ToBranchLocalTime(DateTime.UtcNow, branch.TimeZoneId);
            var targetDate = DateOnly.FromDateTime(nowBranchLocal);
            var targetDay = targetDate.DayOfWeek;

            var businessHours = branch.OperatingHours.FirstOrDefault(h => h.DayOfWeek == targetDay) ?? DayScheduleEntry.OffFor(targetDay);

            var eligibleStaff = tenant.Members
                .Where(member => member.IsActive
                    && member.BranchId == branch.BranchId
                    && targetService.ServiceProviders.Any(prov => prov.TenantMemberId == member.TenantMemberId))
                .OrderBy(m => m.FirstName);

            var results = new List<WalkInStaffAvailability>();

            foreach (var member in eligibleStaff)
            {
                var staffHours = member.WeeklySchedule.FirstOrDefault(s => s.DayOfWeek == targetDay) ?? DayScheduleEntry.OffFor(targetDay);

                var dailyBookings = tenant.Bookings
                    .Where(b => b.BranchId == branch.BranchId
                             && b.StaffId == member.TenantMemberId
                             && b.ScheduledDate == targetDate
                             && b.Status == BookingStatus.Scheduled)
                    .Select(b => new BookingTimeSnapshot(b.ScheduledStartTime, b.ScheduledEndTime))
                    .ToList();

                // Walk-in bookings are exempt from the tenant's minimum-advance-booking policy — the
                // customer is standing at the counter right now, not booking ahead of time. This
                // mirrors TenantMember.IsAvailableAt (used below), which never applies it either.
                var generationResult = _slotGenerator.GenerateAvailableSlots(new SlotGenerationRequest(
                    TargetDate: targetDate,
                    ServiceDurationMinutes: targetService.DurationMinutes,
                    ServiceBufferAfterMinutes: targetService.BufferAfterMinutes,
                    BusinessOperatingHours: businessHours,
                    StaffShiftHours: staffHours,
                    StaffBreaks: member.Breaks,
                    ExistingBookings: dailyBookings,
                    MinAdvanceBookingHours: 0,
                    NowBranchLocal: nowBranchLocal,
                    StaffIsOnApprovedTimeOff: member.HasApprovedTimeOff(targetDate)
                ));

                var slots = generationResult.Slots.OrderBy(s => s).ToList();

                bool isAvailableNow = member.IsAvailableAt(nowBranchLocal, targetService.DurationMinutes, targetService.BufferAfterMinutes, tenant.Bookings);

                string? recommendedDisplay = null;
                TimeOnly? recommendedRaw = null;
                string? availableUntilDisplay = null;
                var alternateTimes = new List<WalkInTimeOption>();

                if (slots.Count > 0)
                {
                    recommendedRaw = slots[0];
                    recommendedDisplay = FormatTime(slots[0]);

                    // Walk the contiguous run of 15-minute slots starting at the recommended time to
                    // find where the current open window actually ends (next break, booking, or the
                    // end of the shift) — whichever the slot generator's own grid already stopped at.
                    var lastContiguous = slots[0];
                    for (int i = 1; i < slots.Count; i++)
                    {
                        if (slots[i] != lastContiguous.AddMinutes(15))
                            break;
                        lastContiguous = slots[i];
                    }
                    availableUntilDisplay = FormatTime(lastContiguous.AddMinutes(targetService.DurationMinutes));

                    alternateTimes = slots.Skip(1)
                        .Select(s => new WalkInTimeOption(FormatTime(s), s))
                        .ToList();
                }

                var unavailableReason = slots.Count == 0
                    ? BuildUnavailableReason(generationResult.Reason)
                    : null;

                results.Add(new WalkInStaffAvailability(
                    TenantMemberId: member.TenantMemberId.Value,
                    FullName: $"{member.FirstName} {member.LastName}".Trim(),
                    CustomJobTitle: member.CustomJobTitle,
                    PhotoUrl: member.PhotoUrl,
                    IsAvailableNow: isAvailableNow,
                    RecommendedTimeDisplay: recommendedDisplay,
                    RecommendedTimeRaw: recommendedRaw,
                    AvailableUntilDisplay: availableUntilDisplay,
                    AlternateTimes: alternateTimes,
                    UnavailableReason: unavailableReason
                ));
            }

            // Earliest next-available time first, so index 0 is the natural "recommended" staff+time
            // pairing; staff with no availability today sort last but are still returned so the UI
            // can show them with their reason instead of silently disappearing.
            return results
                .OrderBy(r => r.RecommendedTimeRaw is null)
                .ThenBy(r => r.RecommendedTimeRaw)
                .ToList();
        }

        private static string FormatTime(TimeOnly time) => DateTime.Today.Add(time.ToTimeSpan()).ToString("hh:mm tt");

        private static string BuildUnavailableReason(SlotUnavailabilityReason reason) => reason switch
        {
            SlotUnavailabilityReason.BranchClosed => "Branch Closed",
            SlotUnavailabilityReason.StaffNotScheduled => "Off Today",
            SlotUnavailabilityReason.StaffOnApprovedTimeOff => "On Time Off",
            SlotUnavailabilityReason.FullyBooked => "Fully Booked",
            _ => "Unavailable"
        };
    }
}
