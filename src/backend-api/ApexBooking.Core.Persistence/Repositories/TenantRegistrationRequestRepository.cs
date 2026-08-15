using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;

namespace ApexBooking.Core.Persistence.Repositories
{
    public class TenantRegistrationRequestRepository(ApexBookingDbContext context) : GenericRepository<TenantRegistrationRequest>(context), ITenantRegistrationRequestRepository
    {
        private ApexBookingDbContext ApexBookingDbContext => Context as ApexBookingDbContext
      ?? throw new InvalidOperationException("The repository context is not an ApexBookingDbContext.");

       
    }
}