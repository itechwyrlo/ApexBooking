using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Customers.Queries.GetAllCustomers
{
    public record GetAllCustomersQuery(QueryObjectParams param) : IQuery<QueryResult<CustomerSummary>>;
}
