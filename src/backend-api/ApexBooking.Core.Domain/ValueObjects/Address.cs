using System.Text.RegularExpressions;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.ValueObjects;

public record Address
{
    public const string PhilippinesCountryName = "Philippines";

    private static readonly Regex PhilippineZipCodePattern = new(@"^\d{4}$", RegexOptions.Compiled);

    public string Street { get; }
    public string? Barangay { get; }
    public string City { get; }
    public string Province { get; }
    public string ZipCode { get; }
    public string Country { get; } = PhilippinesCountryName;

    private Address() : this(string.Empty, null, string.Empty, string.Empty, string.Empty, skipValidation: true) { }

    public Address(string street, string? barangay, string city, string province, string zipCode)
        : this(street, barangay, city, province, zipCode, skipValidation: false) { }

    private Address(string street, string? barangay, string city, string province, string zipCode, bool skipValidation)
    {
        if (!skipValidation)
        {
            if (string.IsNullOrWhiteSpace(street))
                throw new BusinessRuleBrokenException("Street is required.");

            if (string.IsNullOrWhiteSpace(city))
                throw new BusinessRuleBrokenException("City or municipality is required.");

            if (string.IsNullOrWhiteSpace(province))
                throw new BusinessRuleBrokenException("Province is required.");

            if (string.IsNullOrWhiteSpace(zipCode))
                throw new BusinessRuleBrokenException("ZIP code is required.");

            if (!PhilippineZipCodePattern.IsMatch(zipCode.Trim()))
                throw new BusinessRuleBrokenException("A Philippine ZIP code must be exactly four digits.");
        }

        Street = street.Trim();
        Barangay = string.IsNullOrWhiteSpace(barangay) ? null : barangay.Trim();
        City = city.Trim();
        Province = province.Trim();
        ZipCode = zipCode.Trim();
        Country = PhilippinesCountryName;
    }

    public static Address Empty { get; } = new();

    public bool IsEmpty => string.IsNullOrWhiteSpace(Street)
        && string.IsNullOrWhiteSpace(City)
        && string.IsNullOrWhiteSpace(Province)
        && string.IsNullOrWhiteSpace(ZipCode);
}
