using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Tenants.Commands.UpdateTenantProfile
{
    public sealed record UpdateTenantProfileCommand(
        string TenantSlug,
        string? LogoUrl,
        string? AddressLine1,
        string? AddressLine2,
        string? City,
        string? State,
        string? PostalCode,
        string? CountryCode,
        string? Timezone,
        string? CurrencyCode,
        string? WebsiteUrl,
        string? ContactEmail,
        string? ContactPhone,
        string? DateFormat,
        string? TimeFormat,
        string? LanguageCode
    ) : ICommand<BaseResponse<bool>>;
}