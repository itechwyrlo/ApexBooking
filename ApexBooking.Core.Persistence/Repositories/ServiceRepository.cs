using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Repositories
{
    public class ServiceRepository : GenericRepository<Service>, IServiceRepository
    {
        public ServiceRepository(ApexBookingDbContext context) : base(context) { }

        public async Task<Service?> FindByNameAsync(string name)
        {
            return await Context.Set<Service>()
                .FirstOrDefaultAsync(s => s.Name == name);
        }

        public async Task<bool> NameExistsAsync(string name)
        {
            return await Context.Set<Service>()
                .AnyAsync(s => s.Name == name);
        }

        public async Task<List<Service>> GetActiveServicesAsync()
        {
            return await Context.Set<Service>()
                .Include(s => s.ServiceResources)
                .Where(s => s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        /// <summary>
        /// Used by public endpoints where the global tenant query filter
        /// is not applied because no JWT is present. Tenant is filtered
        /// explicitly via TenantId.
        /// </summary>
        public async Task<List<Service>> GetActiveServicesByTenantAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Service>()
                .IgnoreQueryFilters()
                .Where(s => s.TenantId == tenantId && s.IsActive)
                .OrderBy(s => s.Name)
                .ToListAsync(cancellationToken);
        }

        public async Task<List<Service>> GetServicesByResourceAsync(ResourceId resourceId)
        {
            return await Context.Set<Service>()
                .Include(s => s.ServiceResources)
                .Where(s => s.IsActive &&
                           s.ServiceResources.Any(sr => sr.ResourceId == resourceId))
                .OrderBy(s => s.Name)
                .ToListAsync();
        }

        public async Task<Service?> GetServiceWithResourcesAsync(ServiceId serviceId)
        {
            return await Context.Set<Service>()
                .Include(s => s.ServiceResources)
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId);
        }

        public async Task<Service?> FindByIdWithResourcesAsync(
            ServiceId serviceId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Service>()
                .Include(s => s.ServiceResources)
                .FirstOrDefaultAsync(s => s.ServiceId == serviceId, cancellationToken);
        }
    }
}