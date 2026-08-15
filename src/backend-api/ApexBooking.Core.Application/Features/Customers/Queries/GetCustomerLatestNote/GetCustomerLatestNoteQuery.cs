using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Customers.Queries.GetCustomerLatestNote
{
    public record GetCustomerLatestNoteQuery(Guid CustomerId) : IQuery<CustomerLatestNoteDto?>;
}
