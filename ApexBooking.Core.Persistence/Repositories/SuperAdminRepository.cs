using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Repositories;

internal class SuperAdminRepository : GenericRepository<SuperAdmin>, ISuperAdminRepository
{
    public SuperAdminRepository(ApexBookingDbContext context) : base(context) { }

    //unused method
    public async Task<SuperAdmin?> FindByEmailAsync(string email)
    {
        return await Context.Set<SuperAdmin>()
            .FirstOrDefaultAsync(sa => sa.Email == email);
    }

    //unused method
    public async Task<bool> EmailExistsAsync(string email)
    {
        return await Context.Set<SuperAdmin>()
            .AnyAsync(sa => sa.Email == email);
    }

    public async Task<SuperAdmin?> FindByRefreshTokenAsync(string refreshToken)
    {
        return await Context.Set<SuperAdmin>()
            .Include(sa => sa.RefreshTokens)
            .Where(sa => sa.RefreshTokens.Any(rt => rt.Token == refreshToken))
            .FirstOrDefaultAsync();
    }
}