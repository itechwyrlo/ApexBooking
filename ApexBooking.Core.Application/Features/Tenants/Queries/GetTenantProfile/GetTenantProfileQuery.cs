using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenants.Queries.GetTenantProfile
{
    public sealed record GetTenantProfileQuery(string Slug) : IQuery<TenantProfileDto>;
}