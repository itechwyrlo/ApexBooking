using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicServices
{
    public sealed record GetPublicServicesQuery(QueryObjectParams param, string Slug) : IQuery<PagedResult<PublicServiceDto>>;
}