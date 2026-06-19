using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicTenant
{
    public sealed record GetPublicTenantQuery(string Slug) : IQuery<PublicTenantDto>;
}