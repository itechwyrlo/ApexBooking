using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Customers.Queries.GetCustomerBookings
{
    public record GetCustomerBookingsQuery(Guid CustomerId, QueryObjectParams param) : IQuery<QueryResult<CustomerBookingSummary>>;
}
