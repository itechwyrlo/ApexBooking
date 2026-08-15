using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Staff.Queries.GetIdleStaff
{
    public class GetIdleStaffHandler : IQueryHandler<GetIdleStaffQuery, IReadOnlyCollection<IdleStaffDto>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetIdleStaffHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<IdleStaffDto>> Handle(GetIdleStaffQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load idle staff. No authenticated tenant context was found.");

            var rows = await _unitOfWork.TenantRepository.GetIdleStaffAsync(tenantId, cancellationToken);

            return rows.Select(r => new IdleStaffDto(r.TenantMemberId, r.Name, r.PhotoUrl)).ToList();
        }
    }
}
