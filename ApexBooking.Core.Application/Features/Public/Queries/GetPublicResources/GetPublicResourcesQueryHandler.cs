using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Slot;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Application.Resources.Mappings;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicResources
{
    internal sealed class GetPublicResourcesQueryHandler
        : IQueryHandler<GetPublicResourcesQuery, BaseResponse<List<PublicResourceDto>>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly SlotAvailabilityService _slotAvailabilityService;

        public GetPublicResourcesQueryHandler(
            IUnitOfWork unitOfWork,
            SlotAvailabilityService slotAvailabilityService)
        {
            _unitOfWork = unitOfWork;
            _slotAvailabilityService = slotAvailabilityService;
        }

        public async Task<BaseResponse<List<PublicResourceDto>>> Handle(
            GetPublicResourcesQuery query,
            CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);

            if (tenant is null)
                return BaseResponse<List<PublicResourceDto>>.Failure($"Tenant '{query.Slug}' not found.");

            var serviceId = new ServiceId(query.ServiceId);

            var service = await _unitOfWork.ServiceRepository.GetAsync(
                predicate: s => s.ServiceId == serviceId,
                includes: s => s.ServiceResources);

            if (service is null || service.TenantId != tenant.TenantId || !service.IsActive)
                return BaseResponse<List<PublicResourceDto>>.Failure("Service not found.");

            var resourceIds = service.ServiceResources
                .Select(sr => sr.ResourceId)
                .ToList();

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TenantProfile?.Timezone ?? "UTC");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var minHours = service.GetEffectiveMinAdvanceBookingHours(tenant.TenantSettings?.MinAdvanceBookingHours ?? 1);

            var filterByDateTime = query.Date.HasValue && query.Time.HasValue;
            var filterByDateOnly = query.Date.HasValue && !query.Time.HasValue;

            var dtos = new List<PublicResourceDto>();

            foreach (var resourceId in resourceIds)
            {
                var resource = await _unitOfWork.ResourceRepository
                    .FindByIdWithAvailabilityAsync(resourceId, cancellationToken);

                if (resource is null || !resource.IsActive)
                    continue;

                if (filterByDateTime)
                {
                    var activeBookings = await _unitOfWork.BookingRepository
                        .GetActiveBookingsForResourceOnDateAsync(resourceId, query.Date!.Value, cancellationToken);

                    var slots = _slotAvailabilityService.ComputeAvailableSlots(
                        service, resource, query.Date!.Value, activeBookings, now, minHours);

                    var requestedTime = query.Time!.Value.ToString("HH:mm");
                    if (!slots.Contains(requestedTime))
                        continue;
                }
                else if (filterByDateOnly)
                {
                    var activeBookings = await _unitOfWork.BookingRepository
                        .GetActiveBookingsForResourceOnDateAsync(resourceId, query.Date!.Value, cancellationToken);

                    var slots = _slotAvailabilityService.ComputeAvailableSlots(
                        service, resource, query.Date!.Value, activeBookings, now, minHours);

                    if (!slots.Any())
                        continue;
                }

                var services = await _unitOfWork.ServiceRepository.GetAllAsync(
                    filter: s => s.TenantId == tenant.TenantId && s.IsActive &&
                                 s.ServiceResources.Any(sr => sr.ResourceId == resourceId));

                dtos.Add(resource.ToPublicResourceDto(services));
            }

            return BaseResponse<List<PublicResourceDto>>.Success(dtos);
        }
    }
}
