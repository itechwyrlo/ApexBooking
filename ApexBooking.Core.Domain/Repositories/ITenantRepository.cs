using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.GenericRepository.Abstractions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories;

public interface ITenantRepository : IGenericRepository<Tenant>
{
    Task<Tenant> GetTenantSettingsById(TenantId tenantId);
    Task<Tenant?> FindBySlugAsync(string slug);
    Task<Tenant?> FindByEmailAsync(string email);
    Task<bool> SlugExistsAsync(string slug);
    Task<bool> EmailExistsAsync(string email);
    Task<TenantSettings?> GetTenantSettingsAsync(TenantId tenantId, CancellationToken cancellationToken = default);
    Task<TenantProfile?> GetTenantProfileAsync( TenantId tenantId, CancellationToken cancellationToken = default);
}
