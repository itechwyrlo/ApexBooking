using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicResources
{
    public sealed record GetPublicResourcesQuery(
        string Slug,
        Guid ServiceId,
        DateOnly? Date = null,
        TimeOnly? Time = null)
        : IQuery<List<PublicStaffDto>>;
}