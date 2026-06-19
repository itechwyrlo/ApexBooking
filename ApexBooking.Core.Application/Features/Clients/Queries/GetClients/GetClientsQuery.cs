using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Clients.Queries.GetClients
{
    public sealed record GetClientsQuery(QueryObjectParams Param) : IQuery<PagedResult<ClientSummaryDto>>;
}
