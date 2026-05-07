using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;

namespace ApexBooking.Core.Persistence.Repositories
{
    public class BookingRepository : GenericRepository<Booking>, IBookingRepository
    {
        public BookingRepository(ApexBookingDbContext context) : base(context) { }

        public async Task<IReadOnlyList<Booking>> GetActiveBookingsForResourceOnDateAsync(
            ResourceId resourceId,
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Booking>()
                .Where(b =>
                    b.ResourceId == resourceId &&
                    b.ScheduledDate == date &&
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
                .ToListAsync(cancellationToken);
        }
    }
}