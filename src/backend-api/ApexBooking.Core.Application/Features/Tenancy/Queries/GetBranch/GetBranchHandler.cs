using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetBranch
{
    public class GetBranchHandler : IQueryHandler<GetBranchQuery, BranchDetailDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetBranchHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<BranchDetailDto> Handle(GetBranchQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load branch. No authenticated tenant context was found.");

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == tenantId,
                includes: t => t.Branches);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Failed to load branch. Isolated business workspace could not be verified.");

            var branchId = new BranchId(query.BranchId);
            var branch = tenant.Branches.FirstOrDefault(b => b.BranchId == branchId);

            if (branch == null)
                throw new BusinessRuleBrokenException("Branch not found.");

            var operatingHours = branch.OperatingHours
                .OrderBy(h => h.DayOfWeek)
                .Select(h => new OperatingHoursEntryDto(h.DayOfWeek, h.StartTime, h.EndTime, h.IsOff))
                .ToList();

            return new BranchDetailDto(
                branch.BranchId.Value,
                branch.BranchName,
                branch.Address.Street,
                branch.Address.Barangay,
                branch.Address.City,
                branch.Address.Province,
                branch.Address.ZipCode,
                branch.TimeZoneId,
                branch.IsActive,
                operatingHours
            );
        }
    }
}
