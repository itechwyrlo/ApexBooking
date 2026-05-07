using System.Diagnostics.CodeAnalysis;
using ApexBooking.Core.Domain.ValueObjects;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities;

public class TenantProfile
{
    public TenantProfileId TenantProfileId { get; protected set; } = default!;
    public TenantId TenantId { get; protected set; } = default!;
    public string? LogoUrl { get; private set; }
    public string? AddressLine1 { get; private set; }
    public string? AddressLine2 { get; private set; }
    public string? City { get; private set; }
    public string? State { get; private set; }
    public string? PostalCode { get; private set; }
    public string? CountryCode { get; private set; }
    public string Timezone { get; private set; } = string.Empty;
    public string CurrencyCode { get; private set; } = string.Empty;
    public string? WebsiteUrl { get; private set; }
    public string? ContactEmail { get; private set; }
    public string? ContactPhone { get; private set; }
    public string DateFormat { get; private set; } = "YYYY-MM-DD";
    public TimeFormat TimeFormat { get; private set; }
    public string LanguageCode { get; private set; } = "en";
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation property
    public Tenant Tenant { get; private set; } = default!;

    protected TenantProfile() { }

    [SetsRequiredMembers]
    public TenantProfile(TenantId tenantId, string timezone, string currencyCode)
    {
        TenantProfileId = new TenantProfileId(Guid.NewGuid());
        TenantId = tenantId;
        Timezone = timezone;
        CurrencyCode = currencyCode;
        DateFormat = "YYYY-MM-DD";
        TimeFormat = TimeFormat.TwelveHour;
        LanguageCode = "en";
        CreatedAt = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;
    }

    public static TenantProfile Create(TenantId tenantId, string timezone = "UTC", string currencyCode = "USD")
    {
        if (string.IsNullOrWhiteSpace(timezone))
            throw new ArgumentException("Timezone is required.", nameof(timezone));

        if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Length != 3)
            throw new ArgumentException("Currency code must be a valid 3-character ISO 4217 code.", nameof(currencyCode));

        return new TenantProfile(tenantId, timezone, currencyCode);
    }

    public void UpdateProfile(
        string? logoUrl = null,
        string? addressLine1 = null,
        string? addressLine2 = null,
        string? city = null,
        string? state = null,
        string? postalCode = null,
        string? countryCode = null,
        string? timezone = null,
        string? currencyCode = null,
        string? websiteUrl = null,
        string? contactEmail = null,
        string? contactPhone = null,
        string? dateFormat = null,
        TimeFormat? timeFormat = null,
        string? languageCode = null)
    {
        if (!string.IsNullOrWhiteSpace(logoUrl))
            LogoUrl = logoUrl;

        if (!string.IsNullOrWhiteSpace(addressLine1))
            AddressLine1 = addressLine1;

        if (!string.IsNullOrWhiteSpace(addressLine2))
            AddressLine2 = addressLine2;

        if (!string.IsNullOrWhiteSpace(city))
            City = city;

        if (!string.IsNullOrWhiteSpace(state))
            State = state;

        if (!string.IsNullOrWhiteSpace(postalCode))
            PostalCode = postalCode;

        if (!string.IsNullOrWhiteSpace(countryCode) && countryCode.Length == 2)
            CountryCode = countryCode;

        if (!string.IsNullOrWhiteSpace(timezone))
            Timezone = timezone;

        if (!string.IsNullOrWhiteSpace(currencyCode) && currencyCode.Length == 3)
            CurrencyCode = currencyCode;

        if (!string.IsNullOrWhiteSpace(websiteUrl))
            WebsiteUrl = websiteUrl;

        if (!string.IsNullOrWhiteSpace(contactEmail))
            ContactEmail = contactEmail;

        if (!string.IsNullOrWhiteSpace(contactPhone))
            ContactPhone = contactPhone;

        if (!string.IsNullOrWhiteSpace(dateFormat))
            DateFormat = dateFormat;

        if (timeFormat.HasValue)
            TimeFormat = timeFormat.Value;

        if (!string.IsNullOrWhiteSpace(languageCode))
            LanguageCode = languageCode;

        UpdatedAt = DateTime.UtcNow;
    }
}

public enum TimeFormat
{
    TwelveHour,
    TwentyFourHour
}
