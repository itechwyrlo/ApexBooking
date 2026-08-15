using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantRevenue
{
    public class GetTenantRevenueHandler : IQueryHandler<GetTenantRevenueQuery, TenantRevenueDto>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetTenantRevenueHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<TenantRevenueDto> Handle(GetTenantRevenueQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load revenue. No authenticated tenant context was found.");

            var revenue = await _unitOfWork.TenantRepository.GetRevenueAsync(tenantId, query.Date, cancellationToken);

            return new TenantRevenueDto(
                revenue.OnlineAmount,
                revenue.PayInVisitAmount,
                revenue.OnlineAmount + revenue.PayInVisitAmount,
                revenue.CurrencyCode);
        }
    }
}
