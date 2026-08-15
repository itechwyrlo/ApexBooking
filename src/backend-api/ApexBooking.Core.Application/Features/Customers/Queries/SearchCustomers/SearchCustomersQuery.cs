using System.Collections.Generic;
using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Customers.Queries.SearchCustomers
{
    public record SearchCustomersQuery(string Term) : IQuery<IReadOnlyCollection<CustomerSummary>>;
}
