using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetTenantSettings
{
    public sealed record GetTenantSettingsQuery() : IQuery<TenantSettingsDto>;
}