using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Slot;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Availability.Queries
{
    internal sealed class GetAvailableSlotsQueryHandler
        : IQueryHandler<GetAvailableSlotsQuery, BaseResponse<AvailableSlotsDto>>
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

        public async Task<BaseResponse<AvailableSlotsDto>> Handle(
            GetAvailableSlotsQuery query,
            CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);

            if (tenant is null)
                return BaseResponse<AvailableSlotsDto>.Failure($"Tenant '{query.Slug}' not found.");

            var tenantId = tenant.TenantId;
            var serviceId = new ServiceId(query.ServiceId);

            var service = await _unitOfWork.ServiceRepository.GetAsync(
                predicate: s => s.ServiceId == serviceId,
                includes: s => s.ServiceStaffs
            );

            if (service is null)
                return BaseResponse<AvailableSlotsDto>.Failure("Service not found.");

            if (!service.IsActive)
                return BaseResponse<AvailableSlotsDto>.Failure("Service not active.");

            if (service.DurationMinutes <= 0)
                return BaseResponse<AvailableSlotsDto>.Failure("Service duration invalid.");

            if (tenant.TenantSettings == null)
                return BaseResponse<AvailableSlotsDto>.Failure("Tenant settings not found.");

            if (tenant.TenantProfile == null)
                return BaseResponse<AvailableSlotsDto>.Failure("Tenant profile not found.");

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TenantProfile.Timezone);
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            var minHours = service.GetEffectiveMinAdvanceBookingHours(tenant.TenantSettings.MinAdvanceBookingHours);
            var maxDays = service.GetEffectiveMaxAdvanceBookingDays(tenant.TenantSettings.MaxAdvanceBookingDays);

            var earliest = DateOnly.FromDateTime(now.AddHours(minHours));
            var latest = DateOnly.FromDateTime(now.AddDays(maxDays));

            if (query.Date < earliest)
                return BaseResponse<AvailableSlotsDto>.Failure("Requested date is too early.");

            if (query.Date > latest)
                return BaseResponse<AvailableSlotsDto>.Failure("Requested date is too late.");

            // Single resource path — existing behaviour unchanged
            if (query.StaffId.HasValue)
            {
                var staffId = new StaffId(query.StaffId.Value);

                if (!service.IsStaffAssigned(staffId))
                    return BaseResponse<AvailableSlotsDto>.Failure("Resource not assigned to service.");

                var resource = await _unitOfWork.StaffRepository
                    .FindByIdWithAvailabilityAsync(staffId, cancellationToken);

                if (resource is null)
                    return BaseResponse<AvailableSlotsDto>.Failure("Resource not found.");

                if (!resource.IsActive)
                    return BaseResponse<AvailableSlotsDto>.Failure("Resource not active.");

                var activeBookings = await _unitOfWork.BookingRepository
                    .GetActiveBookingsForStaffOnDateAsync(staffId, query.Date, cancellationToken);

                var slots = _slotAvailabilityService.ComputeAvailableSlots(
                    service, resource, query.Date, activeBookings, now, minHours);

                return BaseResponse<AvailableSlotsDto>.Success(
                    AvailabilityMappings.ToAvailableSlotsDto(
                        query.ServiceId, query.StaffId, query.Date, service.DurationMinutes, slots));
            }

            // Union path — no specific resource: return distinct union of all available slots
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

            return BaseResponse<AvailableSlotsDto>.Success(
                AvailabilityMappings.ToAvailableSlotsDto(
                    query.ServiceId, null, query.Date, service.DurationMinutes, unionSlots));
        }
    }
}
