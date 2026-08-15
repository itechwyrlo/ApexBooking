using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Customers.Queries.GetCustomerBookings
{
    public class GetCustomerBookingsHandler : IQueryHandler<GetCustomerBookingsQuery, QueryResult<CustomerBookingSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetCustomerBookingsHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<QueryResult<CustomerBookingSummary>> Handle(GetCustomerBookingsQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load booking history. No authenticated tenant context was found.");

            var pagedResult = await _unitOfWork.TenantRepository.GetCustomerBookingsPageAsync(
                tenantId,
                new CustomerId(query.CustomerId),
                query.param,
                cancellationToken);

            var mappedItems = pagedResult.data.Select(row => new CustomerBookingSummary(
                row.BookingId,
                row.BookingReference,
                row.ServiceName,
                row.StaffName,
                row.BranchName,
                row.ScheduledDate,
                row.ScheduledStartTime,
                row.Status,
                row.RequiresUpfrontPayment,
                row.AmountDue,
                row.CurrencyCode,
                row.PaymentConfirmedVia,
                row.CreatedAt));

            return new QueryResult<CustomerBookingSummary>(mappedItems, pagedResult.total);
        }
    }
}
