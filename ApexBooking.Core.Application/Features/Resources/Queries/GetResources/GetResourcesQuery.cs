using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Resources.Queries.GetResources
{
    public sealed record GetResourcesQuery(QueryObjectParams param) 
    : IQuery<PagedResult<ResourceDto>>;
}