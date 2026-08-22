using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.Core.Persistence.Data;
using ApexBooking.GenericRepository.EntityFramework;
using ApexBooking.SharedKernel.Models;
using Microsoft.EntityFrameworkCore;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Persistence.Repositories;

public class TenantRepository(ApexBookingDbContext context) : GenericRepository<Tenant>(context), ITenantRepository
{
     private ApexBookingDbContext ApexBookingDbContext => Context as ApexBookingDbContext
        ?? throw new InvalidOperationException("The repository context is not an ApexBookingDbContext.");

    public async Task<Tenant> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        return await context.Tenants
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t =>
                t.IsActive &&
                t.Members.Any(m => m.UserId == userId && m.Status == TenantMemberStatus.Active),
                cancellationToken);
    }

    public async Task<Tenant?> GetWithServiceStaffAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Tenants
            .Include(t => t.Services).ThenInclude(s => s.ServiceProviders)
            .Include(t => t.Members)
            .Include(t => t.Branches)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);
    }

    public async Task<Tenant?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default)
    {
        // Resolves the owning tenant in two flat steps rather than filtering Tenant through a
        // t.Bookings.Any(...) navigation predicate, which EF can't translate for TenantId's converted
        // type. IgnoreQueryFilters() is required here too: this lookup runs with no ambient tenant
        // (anonymous webhook traffic), and the global per-tenant filter doesn't translate cleanly when
        // CurrentTenantId is null — but bypassing it is also the semantically correct move, since the
        // whole point of this method is to search across every tenant to find which one owns the booking.
        var bookingIdVo = new BookingId(bookingId);
        var tenantId = await context.Bookings
            .IgnoreQueryFilters()
            .Where(b => b.BookingId == bookingIdVo)
            .Select(b => b.TenantId)
            .FirstOrDefaultAsync(cancellationToken);

        if (tenantId is null)
            return null;

        return await context.Tenants
            .Include(t => t.Bookings)
            .Include(t => t.PaymentCredential)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);
    }

    public async Task<IReadOnlyCollection<Tenant>> GetTenantsWithStalePendingBookingsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default)
    {
        return await context.Tenants
            .IgnoreQueryFilters()
            .Include(t => t.Bookings)
            .Where(t => t.Bookings.Any(b => b.Status == BookingStatus.PendingPayment && b.CreatedAt <= cutoffUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<Tenant?> GetForWalkInAvailabilityAsync(TenantId tenantId, CancellationToken cancellationToken = default)
    {
        return await context.Tenants
            .Include(t => t.Branches)
            .Include(t => t.Members)
            .Include(t => t.Services).ThenInclude(s => s.ServiceProviders)
            .Include(t => t.Bookings)
            .FirstOrDefaultAsync(t => t.TenantId == tenantId, cancellationToken);
    }

    public async Task<QueryResult<TenantBookingRow>> GetBookingsPageAsync(
        TenantId tenantId,
        QueryObjectParams queryObjectParams,
        BranchId? branchId,
        TenantMemberId? staffId,
        BookingStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default)
    {
        var bookings = context.Bookings.AsNoTracking().Where(b => b.TenantId == tenantId);

        if (branchId is not null) bookings = bookings.Where(b => b.BranchId == branchId);
        if (staffId is not null) bookings = bookings.Where(b => b.StaffId == staffId);
        if (status is not null) bookings = bookings.Where(b => b.Status == status);
        if (fromDate is not null) bookings = bookings.Where(b => b.ScheduledDate >= fromDate);
        if (toDate is not null) bookings = bookings.Where(b => b.ScheduledDate <= toDate);

        var rows =
            from b in bookings
            join c in context.Customers.AsNoTracking() on b.CustomerId equals c.CustomerId into customerGroup
            from c in customerGroup.DefaultIfEmpty()
            join m in context.Staffs.AsNoTracking() on b.StaffId equals m.TenantMemberId into staffGroup
            from m in staffGroup.DefaultIfEmpty()
            join s in context.Services.AsNoTracking() on b.ServiceId equals s.ServiceId into serviceGroup
            from s in serviceGroup.DefaultIfEmpty()
            join br in context.Branches.AsNoTracking() on b.BranchId equals br.BranchId into branchGroup
            from br in branchGroup.DefaultIfEmpty()
            orderby b.ScheduledDate descending, b.ScheduledStartTime descending
            select new TenantBookingRow(
                b.BookingId.Value,
                b.BookingReference,
                c != null ? c.Contact.Name : "Unknown",
                c != null ? c.Contact.PhoneNumber : null,
                s != null ? s.Name : "Unknown",
                m != null ? m.FirstName + " " + m.LastName : "Unknown",
                br != null ? br.BranchName : "Unknown",
                b.ScheduledDate,
                b.ScheduledStartTime,
                b.DurationMinutes,
                b.Status,
                b.RequiresUpfrontPayment,
                b.AmountDue,
                b.CurrencyCode,
                b.PaymentConfirmedVia,
                b.CheckedInAt,
                b.ServiceCompletedAt,
                b.CancelledAt,
                b.CancellationReason,
                b.NoShowAt,
                b.CustomerId.Value,
                b.StaffId.Value,
                b.CreatedAt,
                b.ServicePriceAtBooking,
                b.InVisitAmountCollected);

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .Skip((queryObjectParams.PageNumber - 1) * queryObjectParams.PageSize)
            .Take(queryObjectParams.PageSize)
            .ToListAsync(cancellationToken);

        return new QueryResult<TenantBookingRow>(page, total);
    }

    public async Task<BookingCheckoutDetailRow?> GetBookingCheckoutDetailAsync(
        TenantId tenantId,
        Guid bookingId,
        CancellationToken cancellationToken = default)
    {
        var bookingIdVo = new BookingId(bookingId);
        var rows =
            from b in context.Bookings.AsNoTracking().Where(b => b.TenantId == tenantId && b.BookingId == bookingIdVo)
            join c in context.Customers.AsNoTracking() on b.CustomerId equals c.CustomerId into customerGroup
            from c in customerGroup.DefaultIfEmpty()
            join m in context.Staffs.AsNoTracking() on b.StaffId equals m.TenantMemberId into staffGroup
            from m in staffGroup.DefaultIfEmpty()
            join s in context.Services.AsNoTracking() on b.ServiceId equals s.ServiceId into serviceGroup
            from s in serviceGroup.DefaultIfEmpty()
            select new BookingCheckoutDetailRow(
                b.BookingId.Value,
                b.BookingReference,
                c != null ? c.Contact.Name : "Unknown",
                c != null ? c.Contact.Email : null,
                c != null ? c.Contact.PhoneNumber : null,
                s != null ? s.Name : "Unknown",
                m != null ? m.FirstName + " " + m.LastName : "Unknown",
                b.BranchId.Value,
                b.ScheduledDate,
                b.ScheduledStartTime,
                b.Status,
                b.CheckedInAt,
                b.AmountDue,
                b.ServicePriceAtBooking,
                b.InVisitAmountCollected,
                b.PaymentConfirmedVia,
                b.CurrencyCode);

        return await rows.FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<QueryResult<CustomerBookingRow>> GetCustomerBookingsPageAsync(
        TenantId tenantId,
        CustomerId customerId,
        QueryObjectParams queryObjectParams,
        CancellationToken cancellationToken = default)
    {
        var bookings = context.Bookings.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.CustomerId == customerId);

        var rows =
            from b in bookings
            join m in context.Staffs.AsNoTracking() on b.StaffId equals m.TenantMemberId into staffGroup
            from m in staffGroup.DefaultIfEmpty()
            join s in context.Services.AsNoTracking() on b.ServiceId equals s.ServiceId into serviceGroup
            from s in serviceGroup.DefaultIfEmpty()
            join br in context.Branches.AsNoTracking() on b.BranchId equals br.BranchId into branchGroup
            from br in branchGroup.DefaultIfEmpty()
            orderby b.ScheduledDate descending, b.ScheduledStartTime descending
            select new CustomerBookingRow(
                b.BookingId.Value,
                b.BookingReference,
                s != null ? s.Name : "Unknown",
                m != null ? m.FirstName + " " + m.LastName : "Unknown",
                br != null ? br.BranchName : "Unknown",
                b.ScheduledDate,
                b.ScheduledStartTime,
                b.Status,
                b.RequiresUpfrontPayment,
                b.AmountDue,
                b.CurrencyCode,
                b.PaymentConfirmedVia,
                b.CreatedAt);

        var total = await rows.CountAsync(cancellationToken);
        var page = await rows
            .Skip((queryObjectParams.PageNumber - 1) * queryObjectParams.PageSize)
            .Take(queryObjectParams.PageSize)
            .ToListAsync(cancellationToken);

        return new QueryResult<CustomerBookingRow>(page, total);
    }

    public async Task<CustomerLatestNoteRow?> GetLatestStaffNoteAsync(
        TenantId tenantId,
        CustomerId customerId,
        CancellationToken cancellationToken = default)
    {
        return await context.Bookings.AsNoTracking()
            .Where(b => b.TenantId == tenantId
                && b.CustomerId == customerId
                && b.Status == BookingStatus.Completed
                && b.StaffNotes != null)
            .OrderByDescending(b => b.ScheduledDate)
            .ThenByDescending(b => b.ScheduledStartTime)
            .Select(b => new CustomerLatestNoteRow(b.StaffNotes!, b.ScheduledDate))
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task<TenantBookingCountsRow> GetBookingCountsAsync(
        TenantId tenantId,
        DateOnly date,
        CancellationToken cancellationToken = default)
    {
        var todaysBookings = context.Bookings.AsNoTracking()
            .Where(b => b.TenantId == tenantId && b.ScheduledDate == date);

        var pending = await todaysBookings.CountAsync(b => b.Status == BookingStatus.Scheduled && b.CheckedInAt == null, cancellationToken);
        var checkedIn = await todaysBookings.CountAsync(b => b.Status == BookingStatus.Scheduled && b.CheckedInAt != null, cancellationToken);
        var completed = await todaysBookings.CountAsync(b => b.Status == BookingStatus.Completed, cancellationToken);
        var missed = await todaysBookings.CountAsync(b => b.Status == BookingStatus.NoShow, cancellationToken);

        return new TenantBookingCountsRow(pending, checkedIn, completed, missed);
    }

    public async Task<IReadOnlyCollection<IdleStaffRow>> GetIdleStaffAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default)
    {
        return await context.Staffs.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.Status == TenantMemberStatus.Active && !m.MemberServices.Any())
            .Select(m => new IdleStaffRow(m.TenantMemberId.Value, m.FirstName + " " + m.LastName, m.PhotoUrl))
            .ToListAsync(cancellationToken);
    }

    public async Task<TenantRevenueRow> GetRevenueAsync(
        TenantId tenantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        var eligibleBookings = context.Bookings.AsNoTracking()
            .Where(b => b.TenantId == tenantId
                && b.ScheduledDate >= fromDate
                && b.ScheduledDate <= toDate
                && b.Status != BookingStatus.Cancelled
                && b.PaymentConfirmedVia != null);

        var onlineAmount = await eligibleBookings
            .Where(b => b.PaymentConfirmedVia == PaymentConfirmationMethod.Online)
            .SumAsync(b => b.AmountDue - (b.RefundStatus == RefundStatus.Refunded ? (b.RefundedAmount ?? 0) : 0), cancellationToken);

        var payInVisitAmount = await eligibleBookings
            .Where(b => b.PaymentConfirmedVia == PaymentConfirmationMethod.PayInVisit)
            .SumAsync(b => b.AmountDue - (b.RefundStatus == RefundStatus.Refunded ? (b.RefundedAmount ?? 0) : 0), cancellationToken);

        // Deposit-then-remainder bookings never flip PaymentConfirmedVia to PayInVisit (it stays
        // Online, preserving refund-eligibility logic) — this is the only place their in-visit
        // remainder gets counted. Scoped to Online rows only: for a pure pay-at-counter booking,
        // InVisitAmountCollected ends up equal to AmountDue, which the branch above already
        // counts — summing it again here would double it.
        var depositRemainderAmount = await eligibleBookings
            .Where(b => b.PaymentConfirmedVia == PaymentConfirmationMethod.Online)
            .SumAsync(b => b.InVisitAmountCollected, cancellationToken);

        payInVisitAmount += depositRemainderAmount;

        var currencyCode = await eligibleBookings.Select(b => b.CurrencyCode).FirstOrDefaultAsync(cancellationToken) ?? "PHP";

        return new TenantRevenueRow(onlineAmount, payInVisitAmount, currencyCode);
    }

    public async Task<IReadOnlyCollection<StaffPerformanceRow>> GetStaffPerformanceAsync(
        TenantId tenantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default)
    {
        return await context.Staffs.AsNoTracking()
            .Where(m => m.TenantId == tenantId && m.Status == TenantMemberStatus.Active)
            .Select(m => new StaffPerformanceRow(
                m.TenantMemberId.Value,
                m.FirstName + " " + m.LastName,
                m.Appointments.Count(b => b.ScheduledDate >= fromDate && b.ScheduledDate <= toDate && b.Status == BookingStatus.Completed),
                m.Appointments
                    .Where(b => b.ScheduledDate >= fromDate && b.ScheduledDate <= toDate && b.Status == BookingStatus.Completed)
                    .Sum(b => b.AmountDue - (b.RefundStatus == RefundStatus.Refunded ? (b.RefundedAmount ?? 0) : 0)),
                "PHP"))
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> StaffHasBookingsAsync(TenantId tenantId, TenantMemberId staffId, CancellationToken cancellationToken = default)
    {
        return await context.Bookings.AsNoTracking()
            .AnyAsync(b => b.TenantId == tenantId && b.StaffId == staffId, cancellationToken);
    }
}
