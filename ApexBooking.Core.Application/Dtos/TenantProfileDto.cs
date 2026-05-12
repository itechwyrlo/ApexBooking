using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.Dtos
{
    public sealed record TenantProfileDto(
        string? LogoUrl,
        string? AddressLine1,
        string? AddressLine2,
        string? City,
        string? State,
        string? PostalCode,
        string? CountryCode,
        string Timezone = "",
        string CurrencyCode = "",
        string? WebsiteUrl = null,
        string? ContactEmail = null,
        string? ContactPhone = null,
        string DateFormat = "",
        TimeFormat? TimeFormat = null,
        string LanguageCode = ""
    );

}