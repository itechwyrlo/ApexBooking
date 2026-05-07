using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Repositories;

public class TenantRepository : GenericRepository<Tenant>, ITenantRepository
{
    public TenantRepository(ApexBookingDbContext context) : base(context) { }

    public async Task<Tenant?> FindBySlugAsync(string slug)
    {
        return await Context.Set<Tenant>()
            .Include(t => t.TenantProfile)
            .Include(t => t.TenantSettings)
            .FirstOrDefaultAsync(t => t.Slug == slug.ToLowerInvariant());
    }

    public async Task<Tenant?> FindByEmailAsync(string email)
    {
        return await Context.Set<Tenant>()
            .Include(t => t.TenantProfile)
            .Include(t => t.TenantSettings)
            .FirstOrDefaultAsync(t => t.OwnerEmail == email);
    }

    public async Task<bool> SlugExistsAsync(string slug)
    {
        return await Context.Set<Tenant>()
            .AnyAsync(t => t.Slug == slug.ToLowerInvariant());
    }

    public async Task<bool> EmailExistsAsync(string email)
    {
        return await Context.Set<Tenant>()
            .AnyAsync(t => t.OwnerEmail == email);
    }

    public async Task<TenantSettings?> GetTenantSettingsAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<TenantSettings>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
    }

    public async Task<TenantProfile?> GetTenantProfileAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<TenantProfile>()
            .FirstOrDefaultAsync(s => s.TenantId == tenantId, cancellationToken);
    }

    public async Task<Tenant> GetTenantSettingsById(TenantId tenantId)
    {
        return await Context.Set<Tenant>().Include(t => t.TenantSettings).FirstOrDefaultAsync(t => t.TenantId == tenantId);
    }
}