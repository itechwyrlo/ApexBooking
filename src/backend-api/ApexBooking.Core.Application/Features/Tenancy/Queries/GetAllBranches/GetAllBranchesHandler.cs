using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllBranches
{
    public class GetAllBranchesHandler : IQueryHandler<GetAllBranchesQuery, IReadOnlyCollection<BranchAdminSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetAllBranchesHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<BranchAdminSummary>> Handle(GetAllBranchesQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to list branches. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Branches);

            if (tenant is null)
                throw new BusinessRuleBrokenException("Failed to list branches. Workspace environment could not be verified.");

            return tenant.Branches
                .OrderBy(b => b.BranchName)
                .Select(b => new BranchAdminSummary(
                    b.BranchId.Value, b.BranchName,
                    b.Address.Street, b.Address.Barangay, b.Address.City,
                    b.Address.Province, b.Address.ZipCode, b.TimeZoneId, b.IsActive))
                .ToList();
        }
    }
}
