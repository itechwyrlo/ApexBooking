using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicBranches
{
    public class GetPublicBranchesHandler : IQueryHandler<GetPublicBranchesQuery, IReadOnlyCollection<BranchSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetPublicBranchesHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<BranchSummary>> Handle(GetPublicBranchesQuery query, CancellationToken cancellationToken)
        {
            // Tenant does not implement ITenantEntity, so the global EF query filter doesn't cover this
            // lookup — scope explicitly via the ambient ITenantEntity (resolved from the request's
            // {slug} route segment for this anonymous/public endpoint).
            if (_tenantEntity.TenantId is not { } tenantId)
                return System.Array.Empty<BranchSummary>();

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Branches);

            if (tenant is null)
                return System.Array.Empty<BranchSummary>();

            return tenant.Branches
                .Where(b => b.IsActive)
                .OrderBy(b => b.BranchName)
                .Select(b => new BranchSummary(
                    b.BranchId.Value,
                    b.BranchName,
                    b.Address.Street,
                    b.Address.Barangay,
                    b.Address.City,
                    b.Address.Province,
                    b.Address.ZipCode,
                    b.TimeZoneId))
                .ToList();
        }
    }
}
