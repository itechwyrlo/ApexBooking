using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities
{
    /// <summary>
    /// Child of Booking. Immutable. Append-only.
    /// Every status transition on a Booking appends one record here.
    /// Never queried independently — always accessed through Booking.StatusLogs.
    /// TR-10.1 Step 6, TR-10.5
    /// </summary>
    public class BookingStatusLog : ITenantEntity
    {
        public BookingStatusLogId BookingStatusLogId { get; private set; } = default!;
        public BookingId BookingId { get; private set; } = default!;
        public TenantId TenantId { get; private set; } = default!;
        public BookingStatus? PreviousStatus { get; private set; }
        public BookingStatus NewStatus { get; private set; }
        public BookingActor ActorType { get; private set; }
        public string? Reason { get; private set; }
        public DateTime CreatedAt { get; private set; }

        protected BookingStatusLog() { }

        private BookingStatusLog(
            BookingId bookingId,
            TenantId tenantId,
            BookingStatus? previousStatus,
            BookingStatus newStatus,
            BookingActor actorType,
            string? reason)
        {
            BookingStatusLogId = new BookingStatusLogId(Guid.NewGuid());
            BookingId = bookingId;
            TenantId = tenantId;
            PreviousStatus = previousStatus;
            NewStatus = newStatus;
            ActorType = actorType;
            Reason = reason;
            CreatedAt = DateTime.UtcNow;
        }

        internal static BookingStatusLog Create(
            BookingId bookingId,
            TenantId tenantId,
            BookingStatus? previousStatus,
            BookingStatus newStatus,
            BookingActor actorType,
            string? reason = null)
        {
            if (bookingId is null)
                throw new BusinessRuleBrokenException("Booking is required.");

            if (tenantId is null)
                throw new BusinessRuleBrokenException("Tenant is required.");

            return new BookingStatusLog(bookingId, tenantId, previousStatus, newStatus, actorType, reason);
        }
    }
}
