using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using AutoMapper;

namespace ApexBooking.Core.Application.Features.Services.Queries.GetServicesByBranch
{
    // Authenticated-side twin of GetPublicServicesByBranchHandler — same "a service is offered at a
    // branch when at least one qualified, active staff member is deployed there" rule, just scoped
    // via the ambient authenticated tenant instead of a public {slug}. Backs the walk-in flow's
    // service picker, which (per the existing service-to-branch relationship) must not offer a
    // service nobody at the selected branch can actually perform.
    public class GetServicesByBranchHandler : IQueryHandler<GetServicesByBranchQuery, IReadOnlyCollection<ServiceCatalogSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;
        private readonly IMapper _mapper;

        public GetServicesByBranchHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
            _mapper = mapper;
        }

        public async Task<IReadOnlyCollection<ServiceCatalogSummary>> Handle(GetServicesByBranchQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load service catalog. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetWithServiceStaffAsync(tenantId, cancellationToken);
            if (tenant is null)
                throw new BusinessRuleBrokenException("Failed to load service catalog. Isolated tenant context could not be verified.");

            var branch = tenant.Branches.FirstOrDefault(b => b.BranchId.Value == query.BranchId && b.IsActive)
                ?? throw new BusinessRuleBrokenException("The selected branch is unavailable.");

            var staffAtBranch = tenant.Members
                .Where(m => m.IsActive && m.BranchId == branch.BranchId)
                .Select(m => m.TenantMemberId)
                .ToHashSet();

            var services = tenant.Services
                .Where(s => s.IsActive && s.ServiceProviders.Any(p => staffAtBranch.Contains(p.TenantMemberId)))
                .OrderBy(s => s.Name)
                .ToList();

            return _mapper.Map<IReadOnlyCollection<ServiceCatalogSummary>>(services);
        }
    }
}
