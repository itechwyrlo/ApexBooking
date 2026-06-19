using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.GenericRepository.Abstractions;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Repositories
{
    public interface IBookingRepository : IGenericRepository<Booking>
    {
        /// <summary>
        /// Returns all confirmed and pending bookings for a resource on a specific date.
        /// Used by the slot calculator to build the list of blocked time windows.
        ///
        /// Only status Confirmed and Pending are returned — Cancelled and NoShow
        /// bookings must not block availability.
        ///
        /// Hits the index: (tenant_id, resource_id, scheduled_date, status)
        /// TR-9.1 Step 7, TR-2.5
        /// </summary>
        Task<IReadOnlyList<Booking>> GetActiveBookingsForStaffOnDateAsync(
            StaffId staffId,
            DateOnly date,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Booking>> GetActiveBookingsForStaffsInDateRangeAsync(
            TenantId tenantId,
            IEnumerable<StaffId> staffIds,
            DateOnly startDate,
            DateOnly endDate,
            CancellationToken cancellationToken = default);

        Task<int> CountBookingsInMonthAsync(
            TenantId tenantId,
            int year,
            int month,
            CancellationToken cancellationToken = default);

        Task<IReadOnlyList<Booking>> GetBookingsForMonthAsync(
            TenantId tenantId,
            int year,
            int month,
            StaffId? staffId,
            CancellationToken cancellationToken = default);

        Task<int> GetNextBookingSequenceAsync(
            TenantId tenantId,
            int year,
            CancellationToken cancellationToken = default);
    }
}