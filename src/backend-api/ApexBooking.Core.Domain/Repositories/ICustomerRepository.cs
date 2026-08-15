using ApexBooking.Core.Domain.Entities;
using ApexBooking.GenericRepository.Abstractions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories;

/// <summary>Repository for the Customer aggregate root (02 §8, 05 §5).</summary>
public interface ICustomerRepository : IGenericRepository<Customer>
{
    /// <summary>Full customer list for a tenant's customer-management screen (product §15).</summary>
    Task<IEnumerable<Customer>> GetByBusinessIdAsync(TenantId tenantId);

    /// <summary>Backs the customer search on the calendar (product §15).</summary>
    Task<IEnumerable<Customer>> SearchByNameOrPhoneAsync(TenantId tenantId, string term);

    /// <summary>
    /// Exact email/phone match within a business — the public-booking customer match/reuse
    /// (ADR-061). Returns null if no existing customer matches.
    /// </summary>
    Task<Customer?> FindByEmailOrPhoneAsync(TenantId tenantId, string? email, string? phone);
}
