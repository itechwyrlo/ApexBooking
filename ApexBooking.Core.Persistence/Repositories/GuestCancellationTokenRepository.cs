using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Repositories;

public class GuestCancellationTokenRepository : GenericRepository<GuestCancellationToken>, IGuestCancellationTokenRepository
{
    public GuestCancellationTokenRepository(ApexBookingDbContext context) : base(context) { }

    public async Task<GuestCancellationToken?> FindByTokenHashAsync(
        string tokenHash,
        CancellationToken cancellationToken = default)
    {
        return await Context.Set<GuestCancellationToken>()
            .IgnoreQueryFilters()
            .Include(gct => gct.Guest)
                .ThenInclude(g => g.Booking)
            .FirstOrDefaultAsync(gct => gct.TokenHash == tokenHash, cancellationToken);
    }
}
