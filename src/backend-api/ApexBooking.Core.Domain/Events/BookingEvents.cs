using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Events;

public record BookingCompletedDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    Guid CustomerId,
    Guid StaffId,
    Guid ServiceId,
    DateTime CompletedAt
) : IReliableDomainEvent;

public record BookingScheduledDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    Guid CustomerId,
    Guid StaffId,
    Guid ServiceId,
    Guid BranchId,
    DateOnly ScheduledDate,
    TimeOnly ScheduledStartTime,
    DateTime ScheduledAt
) : IReliableDomainEvent;

// Raised once per booking, in both branches of Booking.Create() — "a new booking exists," distinct
// from BookingScheduledDomainEvent ("it's confirmed") and BookingPendingPaymentDomainEvent ("it's
// awaiting payment"), both of which may also fire for the same booking at the same instant.
// Notification-only (no external call), so plain IDomainEvent — stays on the synchronous path.
public record BookingCreatedDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    Guid CustomerId,
    Guid StaffId,
    Guid ServiceId,
    DateTime CreatedAt
) : IDomainEvent;

public record BookingPendingPaymentDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    Guid CustomerId,
    decimal AmountDue,
    string CurrencyCode,
    DateTime CreatedAt
) : IDomainEvent;

public record BookingCancelledDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    Guid CustomerId,
    string Reason,
    // Null when the customer cancelled it themselves (Booking.CancelByCustomer) rather than
    // a staff/admin account (Booking.Cancel).
    Guid? CancelledByUserId,
    DateTime CancelledAt
) : IDomainEvent;

public record BookingNoShowDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    Guid CustomerId,
    Guid StaffId,
    DateTime OccurredAt
) : IDomainEvent;

// Raised the moment a webhook clears payment (Booking.ConfirmPayment) — alongside
// BookingScheduledDomainEvent, which fires at the same call site for the "now confirmed" framing.
// This one carries the amount, for a distinct "payment received" notification.
public record PaymentCapturedDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    decimal AmountDue,
    string CurrencyCode,
    PaymentConfirmationMethod Method,
    DateTime CapturedAt
) : IDomainEvent;

// Raised when a cancellation qualifies for a refund (see Booking.EvaluateRefund) — creates a
// RefundRequest awaiting human review. E-wallet details travel with this event because they're
// captured up front, in the same cancellation request, not as a later follow-up.
public record BookingRefundEligibleDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    decimal RefundAmount,
    string CurrencyCode,
    string EwalletProvider,
    string EwalletNumber,
    string EwalletName,
    DateTime OccurredAt
) : IDomainEvent;

// Raised when an Owner/Admin confirms a manual e-wallet transfer with a receipt as proof —
// drives the customer's "your refund was sent" email. Reliable: the email is an external call.
public record BookingRefundConfirmedDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    decimal RefundedAmount,
    string CurrencyCode,
    string ReceiptUrl,
    DateTime OccurredAt
) : IReliableDomainEvent;

// Drives the customer-facing cancellation email — raised unconditionally alongside
// BookingCancelledDomainEvent (regardless of refund eligibility). The actual refund outcome
// (confirmed/rejected) is always covered separately by a dedicated, reliable email fired
// exactly when that decision happens — see BookingRefundConfirmedDomainEvent and
// BookingRefundRejectedDomainEvent below.
public record BookingCancellationNoticeDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    DateTime CancelledAt
) : IReliableDomainEvent;

// Raised when a refund review is rejected (Booking.RejectReviewedRefund) — drives a dedicated
// customer notice explaining why, separate from the cancellation notice above since it may land
// long after the cancellation itself (the review can take days).
public record BookingRefundRejectedDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    string RejectionReason,
    DateTime OccurredAt
) : IReliableDomainEvent;
