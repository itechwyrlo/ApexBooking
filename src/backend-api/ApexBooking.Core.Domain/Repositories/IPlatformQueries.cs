using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories;

/// <summary>
/// Read-only cross-tenant reporting queries for the SuperAdmin platform dashboard. Deliberately
/// bypasses the per-tenant aggregate boundary (Tenant/Members/Services/Bookings) since these
/// reads span every tenant at once and carry no write-side invariants.
/// </summary>
public interface IPlatformQueries
{
    Task<PlatformDashboardCounts> GetDashboardCountsAsync(CancellationToken cancellationToken = default);

    Task<QueryResult<PlatformBookingRow>> GetBookingsPageAsync(
        QueryObjectParams queryObjectParams,
        TenantId? tenantId,
        BookingStatus? status,
        DateOnly? fromDate,
        DateOnly? toDate,
        CancellationToken cancellationToken = default);
}

public record PlatformDashboardCounts(
    int TotalTenants,
    int ActiveTenants,
    int PendingRequests,
    int ApprovedRequests,
    int RejectedRequests,
    int BookingsToday,
    int BookingsThisMonth
);

public record PlatformBookingRow(
    Guid BookingId,
    string BookingReference,
    Guid TenantId,
    string TenantName,
    string CustomerName,
    string StaffName,
    string ServiceName,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    BookingStatus Status
);
