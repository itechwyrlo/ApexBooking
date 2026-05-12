using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Repositories;

public class TenantRequestRepository : GenericRepository<TenantRequest>, ITenantRequestRepository
{
    public TenantRequestRepository(ApexBookingDbContext context) : base(context) { }

    public async Task<IEnumerable<TenantRequest>> GetPendingAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<TenantRequest>()
            .Where(r => r.Status == TenantRequestStatus.Pending)
            .OrderByDescending(r => r.CreatedAt)
            .ToListAsync(cancellationToken);
    }
}
