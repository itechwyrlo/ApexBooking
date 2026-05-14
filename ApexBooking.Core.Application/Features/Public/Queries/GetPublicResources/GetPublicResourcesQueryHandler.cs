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
        : IQueryHandler<GetPublicResourcesQuery, BaseResponse<List<PublicStaffDto>>>
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

        public async Task<BaseResponse<List<PublicStaffDto>>> Handle(
            GetPublicResourcesQuery query,
            CancellationToken cancellationToken)
        {
            var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);

            if (tenant is null)
                return BaseResponse<List<PublicStaffDto>>.Failure($"Tenant '{query.Slug}' not found.");

            var serviceId = new ServiceId(query.ServiceId);

            var service = await _unitOfWork.ServiceRepository.GetAsync(
                predicate: s => s.ServiceId == serviceId,
                includes: s => s.ServiceStaffs);

            if (service is null || service.TenantId != tenant.TenantId || !service.IsActive)
                return BaseResponse<List<PublicStaffDto>>.Failure("Service not found.");

            var staffIds = service.ServiceStaffs
                .Select(sr => sr.StaffId)
                .ToList();

            var timeZone = TimeZoneInfo.FindSystemTimeZoneById(tenant.TenantProfile?.Timezone ?? "UTC");
            var now = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, timeZone);
            var minHours = service.GetEffectiveMinAdvanceBookingHours(tenant.TenantSettings?.MinAdvanceBookingHours ?? 1);

            var filterByDateTime = query.Date.HasValue && query.Time.HasValue;
            var filterByDateOnly = query.Date.HasValue && !query.Time.HasValue;

            var dtos = new List<PublicStaffDto>();

            foreach (var staffId in staffIds)
            {
                var resource = await _unitOfWork.StaffRepository
                    .FindByIdWithAvailabilityAsync(staffId, cancellationToken);

                if (resource is null || !resource.IsActive)
                    continue;

                if (filterByDateTime)
                {
                    var activeBookings = await _unitOfWork.BookingRepository
                        .GetActiveBookingsForStaffOnDateAsync(staffId, query.Date!.Value, cancellationToken);

                    var slots = _slotAvailabilityService.ComputeAvailableSlots(
                        service, resource, query.Date!.Value, activeBookings, now, minHours);

                    var requestedTime = query.Time!.Value.ToString("HH:mm");
                    if (!slots.Contains(requestedTime))
                        continue;
                }
                else if (filterByDateOnly)
                {
                    var activeBookings = await _unitOfWork.BookingRepository
                        .GetActiveBookingsForStaffOnDateAsync(staffId, query.Date!.Value, cancellationToken);

                    var slots = _slotAvailabilityService.ComputeAvailableSlots(
                        service, resource, query.Date!.Value, activeBookings, now, minHours);

                    if (!slots.Any())
                        continue;
                }

                var services = await _unitOfWork.ServiceRepository.GetAllAsync(
                    filter: s => s.TenantId == tenant.TenantId && s.IsActive &&
                                 s.ServiceStaffs.Any(sr => sr.StaffId == staffId));

                dtos.Add(resource.ToPublicResourceDto(services));
            }

            return BaseResponse<List<PublicStaffDto>>.Success(dtos);
        }
    }
}
