using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Slot;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Availability.Queries
{
    internal sealed class GetAvailableSlotsQueryHandler
        : IQueryHandler<GetAvailableSlotsQuery, AvailableSlotsDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SlotAvailabilityService _slotAvailabilityService;

        public GetAvailableSlotsQueryHandler(
            IUnitOfWork unitOfWork,
            SlotAvailabilityService slotAvailabilityService)
        {
            _unitOfWork = unitOfWork;
            _slotAvailabilityService = slotAvailabilityService;
        }

        public async Task<AvailableSlotsDto> Handle(
            GetAvailableSlotsQuery query,
            CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);
            if (tenant is null)
                throw new NotFoundException($"Tenant '{query.Slug}' not found.");

            var serviceId = new ServiceId(query.ServiceId);

            var service = await _unitOfWork.ServiceRepository.GetAsync(
                predicate: s => s.ServiceId == serviceId,
                includes: s => s.ServiceStaffs);

            if (service is null || !service.IsActive)
                throw new NotFoundException("Service not found.");

            if (tenant.TenantSettings == null)
                throw new NotFoundException("Tenant configuration not found.");

            if (tenant.TenantProfile == null)
                throw new NotFoundException("Tenant configuration not found.");

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TenantProfile.Timezone);
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            var minHours = service.GetEffectiveMinAdvanceBookingHours(tenant.TenantSettings.MinAdvanceBookingHours);
            var maxDays = service.GetEffectiveMaxAdvanceBookingDays(tenant.TenantSettings.MaxAdvanceBookingDays);

            var earliest = DateOnly.FromDateTime(now.AddHours(minHours));
            var latest = DateOnly.FromDateTime(now.AddDays(maxDays));

            service.EnsureDateWithinBookingWindow(query.Date, earliest, latest);

            if (query.StaffId.HasValue)
            {
                var staffId = new StaffId(query.StaffId.Value);

                service.EnsureStaffAssigned(staffId);

                var resource = await _unitOfWork.StaffRepository
                    .FindByIdWithAvailabilityAsync(staffId, cancellationToken);

                if (resource is null)
                    throw new NotFoundException("Staff not found.");

                resource.EnsureAvailableForBooking();

                var activeBookings = await _unitOfWork.BookingRepository
                    .GetActiveBookingsForStaffOnDateAsync(staffId, query.Date, cancellationToken);

                var slots = _slotAvailabilityService.ComputeAvailableSlots(
                    service, resource, query.Date, activeBookings, now, minHours);

                return AvailabilityMappings.ToAvailableSlotsDto(
                    query.ServiceId, query.StaffId, query.Date, service.DurationMinutes, slots);
            }

            var allSlots = new HashSet<string>();

            foreach (var sr in service.ServiceStaffs)
            {
                var staff = await _unitOfWork.StaffRepository
                    .FindByIdWithAvailabilityAsync(sr.StaffId, cancellationToken);

                if (staff is null || !staff.IsActive)
                    continue;

                var activeBookings = await _unitOfWork.BookingRepository
                    .GetActiveBookingsForStaffOnDateAsync(sr.StaffId, query.Date, cancellationToken);

                var slots = _slotAvailabilityService.ComputeAvailableSlots(
                    service, staff, query.Date, activeBookings, now, minHours);

                foreach (var slot in slots)
                    allSlots.Add(slot);
            }

            var unionSlots = allSlots.Order().ToList().AsReadOnly();

            return AvailabilityMappings.ToAvailableSlotsDto(
                query.ServiceId, null, query.Date, service.DurationMinutes, unionSlots);
        }
    }
}
