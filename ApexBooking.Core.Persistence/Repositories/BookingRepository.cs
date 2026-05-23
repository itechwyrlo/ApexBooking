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

        public async Task<IReadOnlyList<Booking>> GetActiveBookingsForStaffOnDateAsync(
            StaffId staffId,
            DateOnly date,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Booking>()
                .Where(b =>
                    b.StaffId == staffId &&
                    b.ScheduledDate == date &&
                    (b.Status == BookingStatus.Confirmed || b.Status == BookingStatus.Pending))
                .ToListAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<Booking>> GetActiveBookingsForStaffsInDateRangeAsync(
            TenantId tenantId,
            IEnumerable<StaffId> staffIds,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default)
        {
            return await Context.Set<Booking>()
                .Where(b => b.TenantId == tenantId &&
                            staffIds.Contains(b.StaffId) &&
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

        public async Task<IReadOnlyList<Booking>> GetBookingsForMonthAsync(
            TenantId tenantId,
            int year,
            int month,
            StaffId? staffId,
            CancellationToken cancellationToken = default)
        {
            var startDate = new DateOnly(year, month, 1);
            var endDate = new DateOnly(year, month, DateTime.DaysInMonth(year, month));

            return await Context.Set<Booking>()
                .Include(b => b.Guest)
                .Where(b =>
                    b.TenantId == tenantId &&
                    b.ScheduledDate >= startDate &&
                    b.ScheduledDate <= endDate &&
                    (staffId == null || b.StaffId == staffId))
                .ToListAsync(cancellationToken);
        }

        public async Task<int> GetNextBookingSequenceAsync(
            TenantId tenantId,
            int year,
            CancellationToken cancellationToken = default)
        {
            var prefix = $"BK-{year}-";
            var references = await Context.Set<Booking>()
                .Where(b => b.TenantId == tenantId && b.BookingReference.StartsWith(prefix))
                .Select(b => b.BookingReference)
                .AsNoTracking()
                .ToListAsync(cancellationToken);

            if (references.Count == 0)
                return 1;

            var max = references
                .Select(r => int.TryParse(r[prefix.Length..], out var n) ? n : 0)
                .Max();

            return max + 1;
        }

    }
}