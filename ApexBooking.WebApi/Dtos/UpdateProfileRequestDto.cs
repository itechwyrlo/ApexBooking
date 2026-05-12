using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.WebApi.Dtos
{
    public class UpdateProfileRequestDto
    {
        public string? LogoUrl { get; init; }
        public string? AddressLine1 { get; init; }
        public string? AddressLine2 { get; init; }
        public string? City { get; init; }
        public string? State { get; init; }
        public string? PostalCode { get; init; }
        public string? CountryCode { get; init; }
        public string? Timezone { get; init; }
        public string? CurrencyCode { get; init; }
        public string? WebsiteUrl { get; init; }
        public string? ContactEmail { get; init; }
        public string? ContactPhone { get; init; }
        public string? DateFormat { get; init; }
        public string? TimeFormat { get; init; }
        public string? LanguageCode { get; init; }
    }
}