using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Staff.Queries.GetStaffPerformance
{
    public class GetStaffPerformanceHandler : IQueryHandler<GetStaffPerformanceQuery, IReadOnlyCollection<StaffPerformanceEntryDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetStaffPerformanceHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<StaffPerformanceEntryDto>> Handle(GetStaffPerformanceQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load staff performance. No authenticated tenant context was found.");

            var rows = await _unitOfWork.TenantRepository.GetStaffPerformanceAsync(tenantId, query.Date, cancellationToken);

            return rows
                .OrderByDescending(r => r.RevenueGenerated)
                .Select(r => new StaffPerformanceEntryDto(r.TenantMemberId, r.Name, r.ServicesCompleted, r.RevenueGenerated, r.CurrencyCode))
                .ToList();
        }
    }
}
