using System;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Slot;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

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
            var resourceId = new ResourceId(query.ResourceId);

            var service = await _unitOfWork.ServiceRepository
                .FindByIdWithResourcesAsync(serviceId, cancellationToken);

            if (service is null)
                return BaseResponse<AvailableSlotsDto>.Failure("Service not found.");

            if (!service.IsActive)
                return BaseResponse<AvailableSlotsDto>.Failure("Service not active.");

            if (service.DurationMinutes <= 0)
                return BaseResponse<AvailableSlotsDto>.Failure("Service duration invalid.");

            var tenantSettings = await _unitOfWork.TenantRepository
                .GetTenantSettingsAsync(tenantId, cancellationToken);

            if (tenantSettings == null) return BaseResponse<AvailableSlotsDto>.Failure("Tenant settings not found.");

            var tenantProfile = await _unitOfWork.TenantRepository
                .GetTenantProfileAsync(tenantId, cancellationToken);
            if (tenantProfile == null) return BaseResponse<AvailableSlotsDto>.Failure("Tenant profile not found.");

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tenantProfile.Timezone);

            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);

            var minHours = service.GetEffectiveMinAdvanceBookingHours(
                tenantSettings.MinAdvanceBookingHours);

            var maxDays = service.GetEffectiveMaxAdvanceBookingDays(
                tenantSettings.MaxAdvanceBookingDays);

            var earliest = DateOnly.FromDateTime(now.AddHours(minHours));
            var latest = DateOnly.FromDateTime(now.AddDays(maxDays));

            if (query.Date < earliest)
                return BaseResponse<AvailableSlotsDto>.Failure("Requested date is too early.");

            if (query.Date > latest)
                return BaseResponse<AvailableSlotsDto>.Failure("Requested date is too late.");

            if (!service.IsResourceAssigned(resourceId))
                return BaseResponse<AvailableSlotsDto>.Failure("Resource not assigned to service.");

            var resource = await _unitOfWork.ResourceRepository
                .FindByIdWithAvailabilityAsync(resourceId, cancellationToken);

            if (resource is null)
                return BaseResponse<AvailableSlotsDto>.Failure("Resource not found.");

            if (!resource.IsActive)
                return BaseResponse<AvailableSlotsDto>.Failure("Resource not active.");

            var activeBookings = await _unitOfWork.BookingRepository
                .GetActiveBookingsForResourceOnDateAsync(
                    resourceId,
                    query.Date,
                    cancellationToken);

            var slots = _slotAvailabilityService.ComputeAvailableSlots(
                service,
                resource,
                query.Date,
                activeBookings);

            return BaseResponse<AvailableSlotsDto>.Success(new AvailableSlotsDto
            {
                ServiceId = query.ServiceId,
                ResourceId = query.ResourceId,
                Date = query.Date,
                DurationMinutes = service.DurationMinutes,
                AvailableSlots = slots
            });
        }
    }
}