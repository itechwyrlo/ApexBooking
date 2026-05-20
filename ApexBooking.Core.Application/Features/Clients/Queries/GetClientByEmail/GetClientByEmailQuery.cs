using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Clients.Queries.GetClientByEmail
{
    public sealed record GetClientByEmailQuery(string Email) : IQuery<ClientDetailDto>;
}
