using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;

namespace ApexBooking.Core.Application.Features.Customers.Queries.SearchCustomers
{
    // Backs the walk-in flow's "find an existing customer" lookup — reuses
    // ICustomerRepository.SearchByNameOrPhoneAsync, which already existed but had no caller.
    public class SearchCustomersHandler : IQueryHandler<SearchCustomersQuery, IReadOnlyCollection<CustomerSummary>>
    {
        private const int MinimumTermLength = 2;

        private readonly IUnitOfWork _unitOfWork;
        private readonly ITenantEntity _tenantEntity;

        public SearchCustomersHandler(IUnitOfWork unitOfWork, ITenantEntity tenantEntity)
        {
            _unitOfWork = unitOfWork;
            _tenantEntity = tenantEntity;
        }

        public async Task<IReadOnlyCollection<CustomerSummary>> Handle(SearchCustomersQuery query, CancellationToken cancellationToken)
        {
            var tenantId = _tenantEntity.TenantId
                ?? throw new BusinessRuleBrokenException("Failed to search clients. No authenticated tenant context was found.");

            var term = (query.Term ?? string.Empty).Trim();
            if (term.Length < MinimumTermLength)
                return System.Array.Empty<CustomerSummary>();

            var matches = await _unitOfWork.CustomerRepository.SearchByNameOrPhoneAsync(tenantId, term);

            return matches
                .Select(c => new CustomerSummary(c.CustomerId.Value, c.Contact.Name, c.Contact.Email, c.Contact.PhoneNumber, c.CreatedAt))
                .ToList();
        }
    }
}
