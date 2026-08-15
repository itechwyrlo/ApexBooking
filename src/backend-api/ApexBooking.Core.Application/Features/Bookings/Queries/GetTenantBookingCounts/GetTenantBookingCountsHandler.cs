using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantBookingCounts
{
    public class GetTenantBookingCountsHandler : IQueryHandler<GetTenantBookingCountsQuery, TenantBookingCountsDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetTenantBookingCountsHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<TenantBookingCountsDto> Handle(GetTenantBookingCountsQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load booking counts. No authenticated tenant context was found.");

            var counts = await _unitOfWork.TenantRepository.GetBookingCountsAsync(tenantId, query.Date, cancellationToken);

            return new TenantBookingCountsDto(counts.Pending, counts.CheckedIn, counts.Completed, counts.Missed);
        }
    }
}
