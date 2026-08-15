using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetPublicServicesByBranch
{
    public class GetPublicServicesByBranchHandler : IQueryHandler<GetPublicServicesByBranchQuery, IReadOnlyCollection<PublicServiceSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetPublicServicesByBranchHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<PublicServiceSummary>> Handle(GetPublicServicesByBranchQuery query, CancellationToken cancellationToken)
        {
 
            var tenant = _tenantEntity.TenantId is { } tenantId
                ? await _unitOfWork.TenantRepository.GetWithServiceStaffAsync(tenantId, cancellationToken)
                : null;

            var branch = tenant?.Branches.FirstOrDefault(b => b.BranchId.Value == query.BranchId && b.IsActive)
                ?? throw new BusinessRuleBrokenException("The selected branch is unavailable.");

        
            var staffAtBranch = tenant!.Members
                .Where(m => m.IsActive && m.BranchId == branch.BranchId)
                .Select(m => m.TenantMemberId)
                .ToHashSet();

            var serviceList = tenant.Services
                .Where(s => s.IsActive && s.ServiceProviders.Any(p => staffAtBranch.Contains(p.TenantMemberId)))
                .OrderBy(s => s.Name)
                .Select(s => new PublicServiceSummary(
                    s.ServiceId.Value,
                    s.Name,
                    s.Description,
                    s.DurationMinutes,
                    s.Price,
                    s.CurrencyCode))
                .ToList();

            return serviceList;
        }
    }
}
