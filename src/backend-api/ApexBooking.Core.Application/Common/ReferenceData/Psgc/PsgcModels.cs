namespace ApexBooking.Core.Application.Common.ReferenceData.Psgc
{
    // Mirrors the PSA-published PSGC dataset (jobuntux/psgc, 2025-2Q release).
    // Highly urbanized cities (e.g. City of Manila) appear in Provinces with CityClass == "HUC"
    // and as their own single entry in Cities pointing back at that pseudo-province code.
    public record PsgcProvince(string ProvCode, string ProvName, string? CityClass);

    public record PsgcCity(string ProvCode, string MunCityCode, string MunCityName);

    public record PsgcBarangay(string MunCityCode, string BrgyCode, string BrgyName);
}
