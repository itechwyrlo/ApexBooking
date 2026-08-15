using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Domain.ValueObjects;

/// <summary>
/// Write-once snapshot of the business details submitted at registration (05 §1).
/// Read exactly once — by the handler that creates the <c>Business</c> aggregate on
/// approval (ADR-054) — then never again.
/// </summary>
public record RegistrationSnapshot
{
    public string BusinessName { get; }
    public BusinessType BusinessType { get; }
    public string? BusinessDescription { get; }

    private RegistrationSnapshot() : this(string.Empty, default, null, skipValidation: true) { }

    public RegistrationSnapshot(string businessName, BusinessType businessType, string? businessDescription = null)
        : this(businessName, businessType, businessDescription, skipValidation: false) { }

    private RegistrationSnapshot(string businessName, BusinessType businessType, string? businessDescription, bool skipValidation)
    {
        if (!skipValidation && string.IsNullOrWhiteSpace(businessName))
            throw new BusinessRuleBrokenException("Business name is required.");

        BusinessName = businessName;
        BusinessType = businessType;
        BusinessDescription = businessDescription;
    }
}
