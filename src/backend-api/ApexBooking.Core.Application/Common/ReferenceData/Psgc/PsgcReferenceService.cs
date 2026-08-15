using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace ApexBooking.Core.Application.Common.ReferenceData.Psgc
{
    // Loaded once as a singleton: the PSGC dataset is static reference data embedded in this
    // assembly (see the .csproj EmbeddedResource entry), not something that changes per-request.
    public class PsgcReferenceService : IPsgcReferenceService
    {
        private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

        private readonly List<PsgcProvince> _provinces;
        private readonly ILookup<string, PsgcCity> _citiesByProvCode;
        private readonly ILookup<string, PsgcBarangay> _barangaysByMunCityCode;
        private readonly Dictionary<string, PsgcProvince> _provinceByName;
        private readonly ILookup<string, PsgcCity> _citiesByName;

        public PsgcReferenceService()
        {
            _provinces = LoadResource<PsgcProvince>("provinces.json");
            var cities = LoadResource<PsgcCity>("muncities.json");
            var barangays = LoadResource<PsgcBarangay>("barangays.json");

            _citiesByProvCode = cities.ToLookup(c => c.ProvCode);
            _barangaysByMunCityCode = barangays.ToLookup(b => b.MunCityCode);
            _provinceByName = _provinces
                .GroupBy(p => Normalize(p.ProvName))
                .ToDictionary(g => g.Key, g => g.First());
            _citiesByName = cities.ToLookup(c => Normalize(c.MunCityName));
        }

        public IReadOnlyCollection<PsgcProvince> GetProvinces() => _provinces;

        public IReadOnlyCollection<PsgcCity> GetCitiesByProvince(string provCode) =>
            _citiesByProvCode[provCode].ToList();

        public IReadOnlyCollection<PsgcBarangay> GetBarangaysByCity(string munCityCode) =>
            _barangaysByMunCityCode[munCityCode].ToList();

        public PsgcValidationError? ValidateAddress(string provinceName, string cityName, string? barangayName)
        {
            if (!_provinceByName.TryGetValue(Normalize(provinceName), out var province))
                return new PsgcValidationError("Province", $"'{provinceName}' is not a recognized province.");

            var city = _citiesByName[Normalize(cityName)].FirstOrDefault(c => c.ProvCode == province.ProvCode);
            if (city == null)
                return new PsgcValidationError("City", $"'{cityName}' is not a recognized city or municipality within '{provinceName}'.");

            if (!string.IsNullOrWhiteSpace(barangayName))
            {
                var barangayExists = _barangaysByMunCityCode[city.MunCityCode]
                    .Any(b => Normalize(b.BrgyName) == Normalize(barangayName));

                if (!barangayExists)
                    return new PsgcValidationError("Barangay", $"'{barangayName}' is not a recognized barangay within '{cityName}'.");
            }

            return null;
        }

        private static string Normalize(string value) => value.Trim().ToUpperInvariant();

        private static List<T> LoadResource<T>(string fileName)
        {
            var assembly = Assembly.GetExecutingAssembly();
            var resourceName = $"{typeof(PsgcReferenceService).Namespace}.{fileName}";

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Embedded PSGC resource '{resourceName}' was not found.");

            return JsonSerializer.Deserialize<List<T>>(stream, JsonOptions)
                ?? throw new InvalidOperationException($"Embedded PSGC resource '{resourceName}' could not be parsed.");
        }
    }
}
