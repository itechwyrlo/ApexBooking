using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Repositories
{
    public class ServiceRepository : GenericRepository<Service>, IServiceRepository
    {
        public ServiceRepository(ApexBookingDbContext context) : base(context) { }

        //unused method
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

       
    }
}