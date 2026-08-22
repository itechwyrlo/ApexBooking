namespace ApexBooking.Core.Domain.Enums
{
    public enum BookingStatus
    {
        PendingPayment = 1,
        // Active calendar block (Used for both future appointments and active walk-ins)
        Scheduled = 2,

        // The service is finished, payment can be captured, and staff is freed up
        Completed = 3,

        // The customer didn't show up within the shop's buffer time; slot was opened up manually
        NoShow = 4,

        // The appointment was called off ahead of time by the user or administrator
        Cancelled = 5,

        // A PendingPayment checkout was abandoned past the stale-payment window and the slot was
        // reclaimed by ExpireStalePendingBookingsJob — see Booking.ExpirePendingPayment().
        Expired = 6
    }
}