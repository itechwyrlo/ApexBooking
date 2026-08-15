using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetBusinessProfile
{
    public record BusinessProfileDto(
        string BusinessName,
        string? Description,
        string? LogoUrl,
        BusinessType BusinessType,
        string? Slug,
        string? ContactPhoneNumber
    );

    public record GetBusinessProfileQuery() : IQuery<BusinessProfileDto>;
}
