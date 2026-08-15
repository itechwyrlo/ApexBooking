using System.Collections.Generic;

namespace ApexBooking.Core.Application.Common.ReferenceData.Psgc
{
    public record PsgcValidationError(string FieldName, string Message);

    public interface IPsgcReferenceService
    {
        IReadOnlyCollection<PsgcProvince> GetProvinces();

        IReadOnlyCollection<PsgcCity> GetCitiesByProvince(string provCode);

        IReadOnlyCollection<PsgcBarangay> GetBarangaysByCity(string munCityCode);

        /// <summary>
        /// Validates that province/city/barangay names form a real, correctly nested PSGC location.
        /// Barangay is skipped when null/blank (it is optional on Address). Returns null when valid,
        /// otherwise the field that failed and a human-readable message.
        /// </summary>
        PsgcValidationError? ValidateAddress(string provinceName, string cityName, string? barangayName);
    }
}
