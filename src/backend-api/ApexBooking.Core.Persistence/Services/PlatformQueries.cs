using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Services;

public sealed class PlatformQueries : IPlatformQueries
{
    private readonly ApexBookingDbContext _context;

    public PlatformQueries(ApexBookingDbContext context) => _context = context;

    public async Task<PlatformDashboardCounts> GetDashboardCountsAsync(CancellationToken cancellationToken = default)
    {
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var monthStart = new DateOnly(today.Year, today.Month, 1);
        var monthEnd = monthStart.AddMonths(1);

        var totalTenants = await _context.Tenants.CountAsync(cancellationToken);
        var activeTenants = await _context.Tenants.CountAsync(t => t.IsActive, cancellationToken);
        var pendingRequests = await _context.TenantRegistrationRequests.CountAsync(r => r.Status == TenantRequestStatus.pending, cancellationToken);
        var approvedRequests = await _context.TenantRegistrationRequests.CountAsync(r => r.Status == TenantRequestStatus.approved, cancellationToken);
        var rejectedRequests = await _context.TenantRegistrationRequests.CountAsync(r => r.Status == TenantRequestStatus.rejected, cancellationToken);
        var bookingsToday = await _context.Bookings.CountAsync(b => b.ScheduledDate == today, cancellationToken);
        var bookingsThisMonth = await _context.Bookings.CountAsync(b => b.ScheduledDate >= monthStart && b.ScheduledDate < monthEnd, cancellationToken);

        return new PlatformDashboardCounts(
            totalTenants, activeTenants,
            pendingRequests, approvedRequests, rejectedRequests,
            bookingsToday, bookingsThisMonth);
    }

    public async Task<QueryResult<PlatformBookingRow>> GetBookingsPageAsync(
        QueryObjectParams queryObjectParams,
        TenantId? tenantId,
        BookingStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var bookings = _context.Bookings.AsNoTracking().AsQueryable();

        if (tenantId is not null) bookings = bookings.Where(b => b.TenantId == tenantId);
        if (status is not null) bookings = bookings.Where(b => b.Status == status);
        if (fromDate is not null) bookings = bookings.Where(b => b.ScheduledDate >= fromDate);
        if (toDate is not null) bookings = bookings.Where(b => b.ScheduledDate <= toDate);

        var rows =
            from b in bookings
            join t in _context.Tenants.AsNoTracking() on b.TenantId equals t.TenantId into tenantGroup
            from t in tenantGroup.DefaultIfEmpty()
            join c in _context.Customers.AsNoTracking() on b.CustomerId equals c.CustomerId into customerGroup
            from c in customerGroup.DefaultIfEmpty()
            join m in _context.Staffs.AsNoTracking() on b.StaffId equals m.TenantMemberId into staffGroup
            from m in staffGroup.DefaultIfEmpty()
            join s in _context.Services.AsNoTracking() on b.ServiceId equals s.ServiceId into serviceGroup
            from s in serviceGroup.DefaultIfEmpty()
            orderby b.ScheduledDate descending, b.ScheduledStartTime descending
            select new PlatformBookingRow(
                b.BookingId.Value,
                b.BookingReference,
                b.TenantId.Value,
                t != null ? t.BusinessProfile.BusinessName : "Unknown",
                c != null ? c.Contact.Name : "Unknown",
                m != null ? m.FirstName + " " + m.LastName : "Unknown",
                s != null ? s.Name : "Unknown",
                b.ScheduledDate,
                b.ScheduledStartTime,
                b.Status);

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .Skip((queryObjectParams.PageNumber - 1) * queryObjectParams.PageSize)
            .Take(queryObjectParams.PageSize)
            .ToListAsync(cancellationToken);

        return new QueryResult<PlatformBookingRow>(page, total);
    }
}
