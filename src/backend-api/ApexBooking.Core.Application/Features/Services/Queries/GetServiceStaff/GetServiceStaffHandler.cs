using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetServiceStaff
{
    public class GetServiceStaffHandler : IQueryHandler<GetServiceStaffQuery, IReadOnlyCollection<StaffAssignmentSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetServiceStaffHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<StaffAssignmentSummary>> Handle(GetServiceStaffQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load service staff. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetWithServiceStaffAsync(tenantId, cancellationToken);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to load service staff. Isolated tenant context could not be verified.");

            var service = tenant.Services.FirstOrDefault(s => s.ServiceId.Value == query.ServiceId)
                ?? throw new BusinessRuleBrokenException("The target service item was not found inside this business catalog.");

            var assignedMemberIds = service.ServiceProviders
                .Select(p => p.TenantMemberId.Value)
                .ToHashSet();

            return tenant.Members
                .Where(m => m.IsActive)
                .OrderBy(m => m.FirstName)
                .Select(m => new StaffAssignmentSummary(
                    TenantMemberId: m.TenantMemberId.Value,
                    FullName: $"{m.FirstName} {m.LastName}".Trim(),
                    CustomJobTitle: m.CustomJobTitle,
                    IsAssigned: assignedMemberIds.Contains(m.TenantMemberId.Value)
                ))
                .ToList();
        }
    }
}
