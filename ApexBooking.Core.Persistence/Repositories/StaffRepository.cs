using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Repositories
{
    public class StaffRepository : GenericRepository<Staff>, IStaffRepository
    {
        public StaffRepository(ApexBookingDbContext context) : base(context) { }

        public async Task<Staff?> FindByIdWithAvailabilityAsync(
            StaffId staffId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Staff>()
                .Include(r => r.AvailabilitySchedules)
                    .ThenInclude(s => s.BreakPeriods)
                .Include(r => r.AvailabilityExceptions)
                .FirstOrDefaultAsync(r => r.StaffId == staffId, cancellationToken);
        }

        public async Task<IEnumerable<Staff>> FindByIdsWithAvailabilityAsync(
            IEnumerable<StaffId> ids,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Staff>()
                .Include(r => r.AvailabilitySchedules)
                    .ThenInclude(s => s.BreakPeriods)
                .Include(r => r.AvailabilityExceptions)
                .Where(r => ids.Contains(r.StaffId))
                .ToListAsync(cancellationToken);
        }

        public async Task<IEnumerable<Staff>> GetByTenantWithAvailabilityAsync(
            TenantId tenantId,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Staff>()
                .Include(r => r.AvailabilitySchedules)
                    .ThenInclude(s => s.BreakPeriods)
                .Include(r => r.AvailabilityExceptions)
                .Where(r => r.TenantId == tenantId)
                .ToListAsync(cancellationToken);
        }

    }
}