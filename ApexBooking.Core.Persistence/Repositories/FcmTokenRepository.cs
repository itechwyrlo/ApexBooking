using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Repositories;

public class FcmTokenRepository : GenericRepository<FcmToken>, IFcmTokenRepository
{
    public FcmTokenRepository(ApexBookingDbContext context) : base(context) { }

    public async Task<IReadOnlyList<string>> GetTokensByUserIdAsync(Guid userId, CancellationToken ct)
    {
        return await Context.Set<FcmToken>()
            .Where(f => f.UserId == userId)
            .Select(f => f.Token)
            .ToListAsync(ct);
    }

    public async Task<FcmToken?> GetByUserAndTokenAsync(Guid userId, string token, CancellationToken ct)
    {
        return await Context.Set<FcmToken>()
            .FirstOrDefaultAsync(f => f.UserId == userId && f.Token == token, ct);
    }

    public async Task DeleteByUserIdAsync(Guid userId, string token, CancellationToken ct)
    {
        await Context.Set<FcmToken>()
            .Where(f => f.UserId == userId && f.Token == token)
            .ExecuteDeleteAsync(ct);
    }
}
