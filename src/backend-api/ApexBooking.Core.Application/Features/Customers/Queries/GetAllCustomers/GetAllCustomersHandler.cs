using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Customers.Queries.GetAllCustomers
{
    public class GetAllCustomersHandler : IQueryHandler<GetAllCustomersQuery, QueryResult<CustomerSummary>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public GetAllCustomersHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<QueryResult<CustomerSummary>> Handle(GetAllCustomersQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to load clients. No authenticated tenant context was found.");

            var pagedResult = await _unitOfWork.CustomerRepository.GetPageAsync(
                query.param,
                predicate: c => c.TenantId == tenantId
            );

            var items = pagedResult.data
                .OrderBy(c => c.Contact.Name)
                .Select(c => new CustomerSummary(
                    c.CustomerId.Value,
                    c.Contact.Name,
                    c.Contact.Email,
                    c.Contact.PhoneNumber,
                    c.CreatedAt
                ));

            return new QueryResult<CustomerSummary>(items, pagedResult.total);
        }
    }
}
