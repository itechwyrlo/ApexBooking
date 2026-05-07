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
        Task<IReadOnlyList<Booking>> GetActiveBookingsForResourceOnDateAsync(
            ResourceId resourceId,
            DateOnly date,
            CancellationToken cancellationToken = default);
    }
}