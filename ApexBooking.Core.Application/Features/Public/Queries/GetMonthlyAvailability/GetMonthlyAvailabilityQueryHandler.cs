using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Slot;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetMonthlyAvailability
{
    internal sealed class GetMonthlyAvailabilityQueryHandler
        : IQueryHandler<GetMonthlyAvailabilityQuery, MonthlyAvailabilityDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SlotAvailabilityService _slotService;

        public GetMonthlyAvailabilityQueryHandler(IUnitOfWork unitOfWork, SlotAvailabilityService slotService)
        {
            _unitOfWork = unitOfWork;
            _slotService = slotService;
        }

        public async Task<MonthlyAvailabilityDto> Handle(
            GetMonthlyAvailabilityQuery query,
            CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);
            if (tenant is null)
                throw new NotFoundException("Tenant not found.");

            var service = await _unitOfWork.ServiceRepository.GetAsync(
                s => s.ServiceId == new ServiceId(query.ServiceId) && s.TenantId == tenant.TenantId,
                s => s.ServiceStaffs);

            if (service is null || !service.IsActive)
                throw new NotFoundException("Service not found.");

            var resourceIds = service.ServiceStaffs.Select(sr => sr.StaffId).ToList();
            if (!resourceIds.Any())
                return new MonthlyAvailabilityDto(query.Year, query.Month, []);

            var resources = (await _unitOfWork.StaffRepository.FindByIdsWithAvailabilityAsync(resourceIds, cancellationToken))
                .Where(r => r.IsActive)
                .ToList();

            if (!resources.Any())
                return new MonthlyAvailabilityDto(query.Year, query.Month, []);

            // Query 4: Bulk load all active bookings for these resources for the entire month
            var firstDay = new DateOnly(query.Year, query.Month, 1);
            var lastDay = firstDay.AddMonths(1).AddDays(-1);

            var bookings = await _unitOfWork.BookingRepository.GetActiveBookingsForStaffsInDateRangeAsync(
                tenant.TenantId,
                resources.Select(r => r.StaffId),
                firstDay,
                lastDay,
                cancellationToken);

            // Compute local "now" for the tenant
            var timezone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TenantProfile?.Timezone ?? "UTC");
            var nowLocal = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timezone);
            var todayLocal = DateOnly.FromDateTime(nowLocal);

            var minHours = tenant.TenantSettings?.MinAdvanceBookingHours ?? 1;
            var maxDays = tenant.TenantSettings?.MaxAdvanceBookingDays ?? 60;
            var minDateTime = nowLocal.AddHours(minHours);
            var minDate = DateOnly.FromDateTime(minDateTime);
            var maxDate = DateOnly.FromDateTime(nowLocal.AddDays(maxDays));

            var days = new List<DayAvailabilityDto>();

            for (var d = firstDay; d <= lastDay; d = d.AddDays(1))
            {
                bool isAvailable = false;

                // Basic window filtering
                if (d >= todayLocal && d >= minDate && d <= maxDate)
                {
                    foreach (var resource in resources)
                    {
                        var schedule = resource.GetScheduleForDay(d.DayOfWeek);
                        if (schedule == null || !schedule.IsAvailable) continue;

                        var bookingsForDay = bookings
                            .Where(b => b.StaffId == resource.StaffId && b.ScheduledDate == d)
                            .ToList();

                        var slots = _slotService.ComputeAvailableSlots(
                            service,
                            resource,
                            d,
                            bookingsForDay,
                            nowLocal,
                            minHours);

                        if (slots.Any())
                        {
                            isAvailable = true;
                            break;
                        }
                    }
                }

                days.Add(new DayAvailabilityDto(d, isAvailable));
            }

            return new MonthlyAvailabilityDto(query.Year, query.Month, days);
        }
    }
}
