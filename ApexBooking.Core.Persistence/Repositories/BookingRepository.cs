using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

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

        public async Task<IReadOnlyList<Booking>> GetActiveBookingsForResourcesInDateRangeAsync(
            TenantId tenantId,
            IEnumerable<ResourceId> resourceIds,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Booking>()
                .Where(b => b.TenantId == tenantId &&
                            resourceIds.Contains(b.ResourceId) &&
                            b.ScheduledDate >= startDate &&
                            b.ScheduledDate <= endDate &&
                            (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountBookingsInMonthAsync(
            TenantId tenantId,
            int year,
            int month,
            CancellationToken cancellationToken = default)
        {
            var startOfMonth = new DateTime(year, month, 1, 0, 0, 0, DateTimeKind.Utc);
            var endOfMonth = startOfMonth.AddMonths(1);
            return await Context.Set<Booking>()
                .CountAsync(b =>
                    b.TenantId == tenantId &&
                    b.CreatedAt >= startOfMonth &&
                    b.CreatedAt < endOfMonth,
                cancellationToken);
        }

    }
}