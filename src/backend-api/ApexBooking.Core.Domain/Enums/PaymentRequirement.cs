namespace ApexBooking.Core.Domain.Enums;

/// <summary>
/// Whether a customer payment is required as part of booking (ADR-060), configured
/// per tenant on <c>Business.BookingPolicy</c>. Enforced by the booking-confirmation
/// handler — a handler-level check, not a Booking invariant.
/// </summary>
public enum PaymentRequirement
{
    /// <summary>Booking is never blocked on payment; tenant collects in person or later. Default.</summary>
    Optional,

    /// <summary>A booking cannot reach Confirmed until a Paid BookingPayment exists.</summary>
    RequiredAtConfirmation
}
