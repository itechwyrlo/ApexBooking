using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Repositories
{
    public class ResourceRepository : GenericRepository<Resource>, IResourceRepository
    {
        public ResourceRepository(ApexBookingDbContext context) : base(context) { }

        public async Task<Resource?> FindByIdWithAvailabilityAsync(
            ResourceId resourceId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Resource>()
                .Include(r => r.AvailabilitySchedules)
                    .ThenInclude(s => s.BreakPeriods)
                .Include(r => r.AvailabilityExceptions)
                .FirstOrDefaultAsync(r => r.ResourceId == resourceId, cancellationToken);
        }

        public async Task<IEnumerable<Resource>> FindByIdsWithAvailabilityAsync(
            IEnumerable<ResourceId> ids,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Resource>()
                .Include(r => r.AvailabilitySchedules)
                    .ThenInclude(s => s.BreakPeriods)
                .Include(r => r.AvailabilityExceptions)
                .Where(r => ids.Contains(r.ResourceId))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Resource>> GetByTenantWithAvailabilityAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Resource>()
                .Include(r => r.AvailabilitySchedules)
                    .ThenInclude(s => s.BreakPeriods)
                .Include(r => r.AvailabilityExceptions)
                .Where(r => r.TenantId == tenantId)
                .ToListAsync(cancellationToken);
        }

    }
}