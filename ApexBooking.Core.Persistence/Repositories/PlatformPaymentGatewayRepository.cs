using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Repositories;

internal class PlatformPaymentGatewayRepository
    : GenericRepository<PlatformPaymentGateway>, IPlatformPaymentGatewayRepository
{
    public PlatformPaymentGatewayRepository(ApexBookingDbContext context) : base(context) { }

    public async Task<PlatformPaymentGateway?> GetActiveAsync(CancellationToken cancellationToken = default)
    {
        return await Context.Set<PlatformPaymentGateway>()
            .FirstOrDefaultAsync(g => g.IsActive, cancellationToken);
    }
}
