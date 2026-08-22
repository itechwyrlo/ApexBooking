using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.GenericRepository.Abstractions;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories;

public interface ITenantRepository : IGenericRepository<Tenant>
{
    Task<Tenant> GetByUserIdAsync(Guid userId, CancellationToken cancellationToken = default);

    // Deliberately resolves the owning tenant via a flat Booking lookup rather than a
    // t.Bookings.Any(...) predicate — that navigation-correlation shape doesn't translate for
    // TenantId's converted type (see TenantRepository for the full explanation). Used by the
    // anonymous PayMongo webhook handler, which has no ambient tenant context to start from.
    Task<Tenant?> GetByBookingIdAsync(Guid bookingId, CancellationToken cancellationToken = default);

    // Cross-tenant sweep query for ExpireStalePendingBookingsJob — same "Booking is a Tenant
    // child, escape-hatch query" rationale as GetByBookingIdAsync above, but returning every
    // matching tenant (with their full Bookings collection loaded) instead of a single one.
    // Runs with no ambient tenant (background job), so the global query filter already passes
    // everything through, but IgnoreQueryFilters() states that intent explicitly rather than
    // relying on it incidentally.
    Task<IReadOnlyCollection<Tenant>> GetTenantsWithStalePendingBookingsAsync(DateTime cutoffUtc, CancellationToken cancellationToken = default);

    // The generic repository's Include() only supports one navigation level deep, so it can't
    // express Services -> ServiceProviders (ThenInclude). Service catalog staff-assignment needs
    // that nested collection populated, hence this purpose-built query — same escape hatch already
    // used by GetByUserIdAsync above for a query the generic method can't express. Also includes
    // Branches: used both by dashboard staff-assignment (branch-deployment check) and the public
    // booking wizard's branch/staff/service lookups.
    Task<Tenant?> GetWithServiceStaffAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    // Same ThenInclude escape hatch as GetWithServiceStaffAsync, but also pulls in Branches and
    // Bookings — everything the walk-in real-time availability check needs in one pass (branch
    // lookup, staff-service assignment, and existing-booking collision detection).
    Task<Tenant?> GetForWalkInAvailabilityAsync(TenantId tenantId, CancellationToken cancellationToken = default);

    // Booking is a Tenant child (not its own IAggregateRoot), so this stays on the aggregate root's
    // own repository rather than a standalone query interface. Bookings is an unbounded, ever-growing
    // table, so — unlike collections small enough to load in full and paginate in memory (team
    // members, time-off requests) — this keeps pagination and the name-lookup joins server-side.
    Task<QueryResult<TenantBookingRow>> GetBookingsPageAsync(
        TenantId tenantId,
        QueryObjectParams queryObjectParams,
        BranchId? branchId,
        TenantMemberId? staffId,
        BookingStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);

    // Same rationale as GetBookingsPageAsync, scoped to one customer's booking history. Customer is
    // its own aggregate root (see ICustomerRepository), but the data being paginated here is Booking
    // — a Tenant child — so it lives here, not on ICustomerRepository.
    Task<QueryResult<CustomerBookingRow>> GetCustomerBookingsPageAsync(
        TenantId tenantId,
        CustomerId customerId,
        QueryObjectParams queryObjectParams,
        CancellationToken cancellationToken = default);

    // Powers the Staff Dashboard's "active appointment" note preview — the single most recent
    // staff-authored note for this customer, if any exists. Same rationale as
    // GetCustomerBookingsPageAsync: Booking is a Tenant child, so this lives here.
    Task<CustomerLatestNoteRow?> GetLatestStaffNoteAsync(
        TenantId tenantId,
        CustomerId customerId,
        CancellationToken cancellationToken = default);

    // Booking.StaffId is DeleteBehavior.Restrict against TenantMember, so this is the up-front
    // check that decides hard-delete vs. soft-delete (deactivate) for a team member. Scoped by
    // tenantId too so this can't be used to probe booking history for a staffId outside the
    // caller's own tenant.
    Task<bool> StaffHasBookingsAsync(TenantId tenantId, TenantMemberId staffId, CancellationToken cancellationToken = default);

    // Powers the Admin Dashboard's Daily Booking Counters — four operational buckets derived
    // from existing Status/CheckedInAt fields, not a new concept. Mirrors PlatformQueries'
    // flat-CountAsync-per-metric style rather than a single GroupBy.
    Task<TenantBookingCountsRow> GetBookingCountsAsync(
        TenantId tenantId,
        DateOnly date,
        CancellationToken cancellationToken = default);

    // Powers the Admin Dashboard's Idle Staff alert — active team members assigned to zero
    // services, and therefore invisible to the public booking wizard (which only lists staff
    // already qualified for a service). A flat query against context.Staffs, not a full Tenant
    // aggregate load — TenantMember.MemberServices is directly navigable.
    Task<IReadOnlyCollection<IdleStaffRow>> GetIdleStaffAsync(
        TenantId tenantId,
        CancellationToken cancellationToken = default);

    // Powers the Owner Dashboard's Total Shop Revenue widget — collected money for the selected
    // period (Today/This Week/This Month), split Online vs. Pay-on-Visit, netted against any
    // succeeded refunds. Same ScheduledDate window GetBookingCountsAsync uses for "today" when
    // fromDate == toDate.
    Task<TenantRevenueRow> GetRevenueAsync(
        TenantId tenantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);

    // Powers the scanner's checkout preview panel — same flat customer/staff/service join
    // GetBookingsPageAsync already uses, scoped to a single booking instead of a page.
    Task<BookingCheckoutDetailRow?> GetBookingCheckoutDetailAsync(
        TenantId tenantId,
        Guid bookingId,
        CancellationToken cancellationToken = default);

    // Powers the Owner Dashboard's Staff Performance List — every active staff member, ranked
    // by completed services and net revenue generated over the selected period (zero for anyone
    // with none, so a solo-operator tenant just sees their own single row with no special-casing
    // needed).
    Task<IReadOnlyCollection<StaffPerformanceRow>> GetStaffPerformanceAsync(
        TenantId tenantId,
        DateOnly fromDate,
        DateOnly toDate,
        CancellationToken cancellationToken = default);
}

public record TenantBookingRow(
    Guid BookingId,
    string BookingReference,
    string CustomerName,
    string? CustomerPhone,
    string ServiceName,
    string StaffName,
    string BranchName,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    int DurationMinutes,
    BookingStatus Status,
    bool RequiresUpfrontPayment,
    decimal AmountDue,
    string CurrencyCode,
    PaymentConfirmationMethod? PaymentConfirmedVia,
    DateTime? CheckedInAt,
    DateTime? ServiceCompletedAt,
    DateTime? CancelledAt,
    string? CancellationReason,
    DateTime? NoShowAt,
    Guid CustomerId,
    Guid StaffId,
    DateTime CreatedAt,
    decimal? ServicePriceAtBooking,
    decimal InVisitAmountCollected
);

public record BookingCheckoutDetailRow(
    Guid BookingId,
    string BookingReference,
    string CustomerName,
    string? CustomerEmail,
    string? CustomerPhone,
    string ServiceName,
    string StaffName,
    Guid BranchId,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    BookingStatus Status,
    DateTime? CheckedInAt,
    decimal AmountDue,
    decimal? ServicePriceAtBooking,
    decimal InVisitAmountCollected,
    PaymentConfirmationMethod? PaymentConfirmedVia,
    string CurrencyCode
);

public record CustomerBookingRow(
    Guid BookingId,
    string BookingReference,
    string ServiceName,
    string StaffName,
    string BranchName,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    BookingStatus Status,
    bool RequiresUpfrontPayment,
    decimal AmountDue,
    string CurrencyCode,
    PaymentConfirmationMethod? PaymentConfirmedVia,
    DateTime CreatedAt
);

public record CustomerLatestNoteRow(string Notes, DateOnly NotedOn);

public record TenantBookingCountsRow(int Pending, int CheckedIn, int Completed, int Missed);

public record IdleStaffRow(Guid TenantMemberId, string Name, string? PhotoUrl);

public record TenantRevenueRow(decimal OnlineAmount, decimal PayInVisitAmount, string CurrencyCode);

public record StaffPerformanceRow(Guid TenantMemberId, string Name, int ServicesCompleted, decimal RevenueGenerated, string CurrencyCode);
