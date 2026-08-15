using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetReassignableStaff
{
    public class GetReassignableStaffHandler : IQueryHandler<GetReassignableStaffQuery, IReadOnlyCollection<ReassignableStaffDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetReassignableStaffHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<ReassignableStaffDto>> Handle(GetReassignableStaffQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load reassignable staff. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetForWalkInAvailabilityAsync(tenantId, cancellationToken);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Failed to load reassignable staff. Isolated tenant context could not be verified.");

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == query.BookingId)
                ?? throw new BusinessRuleBrokenException("Target appointment log was not found within this business ledger.");

            var service = tenant.Services.FirstOrDefault(s => s.ServiceId == booking.ServiceId);
            if (service is null)
                return Array.Empty<ReassignableStaffDto>();

            return tenant.Members
                .Where(member => member.IsActive &&
                                 member.BranchId == booking.BranchId &&
                                 service.ServiceProviders.Any(prov => prov.TenantMemberId == member.TenantMemberId))
                .OrderBy(m => m.FirstName)
                .Select(m => new ReassignableStaffDto(m.TenantMemberId.Value, $"{m.FirstName} {m.LastName}".Trim()))
                .ToList();
        }
    }
}
