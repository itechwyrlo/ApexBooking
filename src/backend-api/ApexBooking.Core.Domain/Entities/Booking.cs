using System;
using System.Collections.Generic;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;
using ApexBooking.SharedKernel.Services;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Domain.Entities
{
    public class Booking : IHasDomainEvents, ITenantEntity
    {
        public BookingId BookingId { get; protected set; } = default!;
        public TenantId TenantId { get; private set; } = default!;
        public BranchId BranchId { get; private set; } = default!;
        public CustomerId CustomerId { get; private set; } = default!;
        public TenantMemberId StaffId { get; private set; } = default!;
        public ServiceId ServiceId { get; private set; } = default!; // 🌟 Snapshot reference context
        public string BookingReference { get; private set; } = string.Empty;

        // Time Tracking & Slot Computation Metadata
        public DateOnly ScheduledDate { get; private set; }
        public TimeOnly ScheduledStartTime { get; private set; }
        public int DurationMinutes { get; private set; }     // Snapshotted at creation to protect history
        public int BufferAfterMinutes { get; private set; }   // Snapshotted at creation to protect history

        // Calculated read-only timeline windows for your stateless slot utility
        public TimeOnly ScheduledEndTime => ScheduledStartTime.AddMinutes(DurationMinutes);
        public TimeOnly TotalBlockedEndTime => ScheduledStartTime.AddMinutes(DurationMinutes + BufferAfterMinutes);

        public decimal RemainingBalance => ComputeRemainingBalance();

        public BookingStatus Status { get; private set; }
        public string? CustomerNotes { get; private set; }
        public string? StaffNotes { get; private set; }

        // Payment Snapshot — captured once at booking time so later PaymentPolicy/Service price
        // changes never rewrite this booking's history.
        public bool RequiresUpfrontPayment { get; private set; }
        public decimal AmountDue { get; private set; }
        public string CurrencyCode { get; private set; } = string.Empty;
        public PaymentConfirmationMethod? PaymentConfirmedVia { get; private set; }
        public string? PayMongoPaymentId { get; private set; }

        // Refund tracking — only ever set for bookings that were RequiresUpfrontPayment +
        // PaymentConfirmedVia == Online at cancellation time. See EvaluateRefund below.
        public RefundStatus RefundStatus { get; private set; } = RefundStatus.None;
        public decimal? RefundedAmount { get; private set; }
        public DateTime? RefundedAt { get; private set; }

        // Core Audit Fields
        public string? CancellationReason { get; private set; }
        public DateTime? CancelledAt { get; private set; }
        public Guid? CancelledByUserId { get; private set; }
        public DateTime? ServiceCompletedAt { get; private set; } // Tracked when set to done
        public DateTime? NoShowAt { get; private set; } // Tracked when flagged as a no-show
        public BookingId? RescheduledFromBookingId { get; private set; }
        public DateTime? CheckedInAt { get; private set; } // QR pass scan timestamp

        // Full service price at booking time — always snapshotted (not just for deposit
        // policies), so RemainingBalance can be computed correctly regardless of payment policy.
        // Nullable because bookings created before this shipped won't have it.
        public decimal? ServicePriceAtBooking { get; private set; }

        // Cash collected in person, tracked separately and additively to the online-charged
        // AmountDue/PaymentConfirmedVia==Online — this is what makes a deposit-then-remainder
        // booking representable at all (see CaptureRemainingInVisitPayment).
        public decimal InVisitAmountCollected { get; private set; } = 0m;

        public DateTime CreatedAt { get; private set; }
        public DateTime UpdatedAt { get; private set; }

        private readonly List<IDomainEvent> _domainEvents = new();
        public IReadOnlyCollection<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();
        public void ClearDomainEvents() => _domainEvents.Clear();
        public void RemoveDomainEvent(IDomainEvent domainEvent) => _domainEvents.Remove(domainEvent);
        private void AddDomainEvent(IDomainEvent domainEvent) => _domainEvents.Add(domainEvent);

        protected Booking() { }

        // Cleaned public/internal factory method matching your Tenant aggregation mechanics
        public static Booking Create(
            TenantId tenantId,
            BranchId branchId,
            CustomerId customerId,
            TenantMemberId staffId,
            ServiceId serviceId,
            string bookingReference,
            DateOnly scheduledDate,
            TimeOnly scheduledStartTime,
            int durationMinutes,
            int bufferAfterMinutes,
            string? customerNotes,
            bool requiresUpfrontPayment, // 🌟 Added parameter flag to route events safely
            string currencyCode,
            decimal amountDue = 0m,
            decimal? servicePriceAtBooking = null)
        {
            if (tenantId is null) throw new ArgumentNullException(nameof(tenantId));
            if (branchId is null) throw new ArgumentNullException(nameof(branchId));
            if (customerId is null) throw new ArgumentNullException(nameof(customerId));
            if (staffId is null) throw new ArgumentNullException(nameof(staffId));
            if (serviceId is null) throw new ArgumentNullException(nameof(serviceId));
            if (string.IsNullOrWhiteSpace(bookingReference)) throw new BusinessRuleBrokenException("Booking reference is required.");
            if (durationMinutes <= 0) throw new BusinessRuleBrokenException("Service duration must be greater than zero.");
            if (bufferAfterMinutes < 0) throw new BusinessRuleBrokenException("Buffer time extensions cannot be negative.");
            if (amountDue < 0) throw new BusinessRuleBrokenException("Amount due cannot be negative.");
            if (string.IsNullOrWhiteSpace(currencyCode) || currencyCode.Trim().Length != 3)
                throw new BusinessRuleBrokenException("A valid 3-character ISO 4217 currency code is required.");

            var booking = new Booking
            {
                BookingId = new BookingId(Guid.NewGuid()),
                TenantId = tenantId,
                BranchId = branchId,
                CustomerId = customerId,
                StaffId = staffId,
                ServiceId = serviceId,
                BookingReference = bookingReference.ToUpperInvariant().Trim(),
                ScheduledDate = scheduledDate,
                ScheduledStartTime = scheduledStartTime,
                DurationMinutes = durationMinutes,
                BufferAfterMinutes = bufferAfterMinutes,
                CustomerNotes = customerNotes?.Trim(),
                RequiresUpfrontPayment = requiresUpfrontPayment,
                AmountDue = amountDue,
                ServicePriceAtBooking = servicePriceAtBooking,
                CurrencyCode = currencyCode.ToUpperInvariant().Trim(),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // Fires for every booking regardless of payment path — see BookingCreatedDomainEvent's
            // doc comment for how this differs from Scheduled/PendingPayment, which may also fire
            // below for this same booking.
            booking.AddDomainEvent(new BookingCreatedDomainEvent(
                TenantId: booking.TenantId,
                BookingId: booking.BookingId.Value,
                BookingReference: booking.BookingReference,
                CustomerId: booking.CustomerId.Value,
                StaffId: booking.StaffId.Value,
                ServiceId: booking.ServiceId.Value,
                CreatedAt: booking.CreatedAt
            ));

            // 🌟 ROUTING INVARIANT STATES
            if (requiresUpfrontPayment)
            {
                booking.Status = BookingStatus.PendingPayment;
                // No BookingScheduledDomainEvent here yet, because the money has not cleared!

                booking.AddDomainEvent(new BookingPendingPaymentDomainEvent(
                    TenantId: booking.TenantId,
                    BookingId: booking.BookingId.Value,
                    BookingReference: booking.BookingReference,
                    CustomerId: booking.CustomerId.Value,
                    AmountDue: booking.AmountDue,
                    CurrencyCode: booking.CurrencyCode,
                    CreatedAt: booking.CreatedAt
                ));
            }
            else
            {
                booking.Status = BookingStatus.Scheduled;
                // PaymentConfirmedVia stays null here — pay-in-visit bookings (walk-ins and
                // no-upfront-payment public bookings alike) aren't confirmed as paid until
                // CompleteService() runs, or earlier via the arrival-scan fallback.

                // 🌟 AUTOMATIC CONFIRMATION EVENT: Raised instantly during instantiation
                booking.AddDomainEvent(new BookingScheduledDomainEvent(
                    TenantId: booking.TenantId,
                    BookingId: booking.BookingId.Value,
                    BookingReference: booking.BookingReference,
                    CustomerId: booking.CustomerId.Value,
                    StaffId: booking.StaffId.Value,
                    ServiceId: booking.ServiceId.Value,
                    BranchId: booking.BranchId.Value,
                    ScheduledDate: booking.ScheduledDate,
                    ScheduledStartTime: booking.ScheduledStartTime,
                    ScheduledAt: booking.CreatedAt
                ));
            }

            return booking;
        }

        // ── Webhook Gateway State Transition ───────────────────────────────────
        // payMongoPaymentId is only ever passed for the Online path (the webhook handler) — the
        // arrival-scan PayInVisit fallback (Tenant.RecordBookingArrival) has no PayMongo payment
        // to reference, so it correctly omits it and this stays null for that booking forever.
        public void ConfirmPayment(PaymentConfirmationMethod method, string? payMongoPaymentId = null)
        {
            if (Status != BookingStatus.PendingPayment)
                throw new BusinessRuleBrokenException("Only appointments pending payment can have their transactions verified.");

            Status = BookingStatus.Scheduled;
            PaymentConfirmedVia = method;
            PayMongoPaymentId = payMongoPaymentId;
            UpdatedAt = DateTime.UtcNow;

            // 🌟 PAID CONFIRMATION EVENT: Raised the exact millisecond the PayMongo Webhook clears it!
            AddDomainEvent(new BookingScheduledDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CustomerId: this.CustomerId.Value,
                StaffId: this.StaffId.Value,
                ServiceId: this.ServiceId.Value,
                BranchId: this.BranchId.Value,
                ScheduledDate: this.ScheduledDate,
                ScheduledStartTime: this.ScheduledStartTime,
                ScheduledAt: DateTime.UtcNow
            ));

            AddDomainEvent(new PaymentCapturedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                AmountDue: this.AmountDue,
                CurrencyCode: this.CurrencyCode,
                Method: method,
                CapturedAt: DateTime.UtcNow
            ));
        }

        // ── Secure QR Boarding Pass Check-In ────────────────────────────────────

        // Returns whether this call actually admitted the booking just now (true) vs it was
        // already checked in and this was a no-op re-scan (false) — callers use this to tell
        // staff "admitted" apart from "already admitted", instead of reporting both as success.
        internal bool RecordArrival()
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Only confirmed, scheduled appointments can be checked in.");

            if (CheckedInAt is not null) return false; // idempotent: re-scanning is harmless

            CheckedInAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
            return true;
        }

        // Replaces the old "auto-confirm as PayInVisit at admit time" behavior — that claimed
        // payment was captured before checkout even existed, at a snapshot amount that's wrong
        // for deposit bookings. This just clears the way for check-in (PendingPayment requires
        // Scheduled status) without asserting anything about payment. Scheduled +
        // PaymentConfirmedVia == null is already the normal starting state for every
        // no-upfront-payment booking — this just reaches it via one more path. The real capture
        // now happens for real at checkout, through CaptureRemainingInVisitPayment.
        internal void ClearPendingPaymentOnArrival()
        {
            if (Status != BookingStatus.PendingPayment)
                throw new BusinessRuleBrokenException("Only bookings pending payment can be cleared for arrival.");

            Status = BookingStatus.Scheduled;
            UpdatedAt = DateTime.UtcNow;
        }

        // ── Minimalist Lean Administration Domain State Machines ──────────────

        private decimal ComputeRemainingBalance()
        {
            if (ServicePriceAtBooking is null)
                return PaymentConfirmedVia is null ? AmountDue : 0m;

            var amountPaidOnline = PaymentConfirmedVia == PaymentConfirmationMethod.Online ? AmountDue : 0m;
            return Math.Max(0m, ServicePriceAtBooking.Value - amountPaidOnline - InVisitAmountCollected);
        }

        // Shared by RecordPayInVisitPayment and CompleteService's auto-settle — the single place
        // any "what's still owed gets captured now" event fires, so both paths report the
        // accurate just-captured amount, not a stale full-booking snapshot. A no-op (not an
        // error) when nothing remains — CompleteService calls this unconditionally and relies on
        // that no-op behavior for already-fully-paid bookings.
        private void CaptureRemainingInVisitPayment()
        {
            var remaining = ComputeRemainingBalance();
            if (remaining <= 0m) return;

            InVisitAmountCollected += remaining;
            PaymentConfirmedVia ??= PaymentConfirmationMethod.PayInVisit; // never overwrites an existing Online

            AddDomainEvent(new PaymentCapturedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                AmountDue: remaining,
                CurrencyCode: this.CurrencyCode,
                Method: PaymentConfirmationMethod.PayInVisit,
                CapturedAt: UpdatedAt
            ));
        }

        public void CompleteService()
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Only active, scheduled appointments can be marked as completed.");

            Status = BookingStatus.Completed;
            ServiceCompletedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            // Whatever remains unpaid at completion is collected right here — the visit's
            // completion IS the payment-collection moment for anything not already settled
            // online (pay-at-counter bookings, and any deposit-required booking's remainder).
            // A no-op when nothing remains.
            CaptureRemainingInVisitPayment();

            AddDomainEvent(new BookingCompletedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CustomerId: this.CustomerId.Value,
                StaffId: this.StaffId.Value,
                ServiceId: this.ServiceId.Value,
                CompletedAt: ServiceCompletedAt.Value
            ));
        }

        // Distinct from CustomerNotes (set once at booking creation, never touched again) — this
        // is post-service, staff-authored, and mutable. Only makes sense once the visit is done.
        public void SetStaffNotes(string notes)
        {
            if (Status != BookingStatus.Completed)
                throw new BusinessRuleBrokenException("Chair notes can only be added to a completed appointment.");

            if (string.IsNullOrWhiteSpace(notes))
                throw new BusinessRuleBrokenException("Chair notes cannot be empty.");

            StaffNotes = notes.Trim();
            UpdatedAt = DateTime.UtcNow;
        }

        public void Reassign(TenantMemberId newStaffId)
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Only currently scheduled appointments can be reassigned.");

            StaffId = newStaffId;
            UpdatedAt = DateTime.UtcNow;
        }

        public void RecordPayInVisitPayment()
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Payment can only be recorded for a currently scheduled appointment.");

            if (ComputeRemainingBalance() <= 0m)
                throw new BusinessRuleBrokenException("Payment has already been recorded for this appointment.");

            UpdatedAt = DateTime.UtcNow;
            CaptureRemainingInVisitPayment();
        }

        public void MarkAsNoShow()
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Only currently active, pending appointments can be marked as a no-show.");

            Status = BookingStatus.NoShow;
            NoShowAt = DateTime.UtcNow;
            UpdatedAt = NoShowAt.Value;

            AddDomainEvent(new BookingNoShowDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CustomerId: this.CustomerId.Value,
                StaffId: this.StaffId.Value,
                OccurredAt: UpdatedAt
            ));
        }

        public void Cancel(Guid adminUserId, string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Cannot cancel an appointment that is already wrapped up or resolved.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleBrokenException("A cancellation reason trace parameter is required.");

            Status = BookingStatus.Cancelled;
            CancellationReason = reason.Trim();
            CancelledAt = DateTime.UtcNow;
            CancelledByUserId = adminUserId;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BookingCancelledDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CustomerId: this.CustomerId.Value,
                Reason: CancellationReason,
                CancelledByUserId: adminUserId,
                CancelledAt: CancelledAt.Value
            ));

            AddDomainEvent(new BookingCancellationNoticeDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CancelledAt: CancelledAt.Value
            ));

            RaiseRefundEligibilityIfAny(bookingPolicy, paymentPolicy, ewalletProvider, ewalletNumber, ewalletName);
        }

        // Customer-initiated cancellation via the emailed cancel link — no staff account behind
        // it, so CancelledByUserId stays null. The notice-window check (CancellationCutoffHours)
        // happens one level up, in Tenant.CancelBookingByCustomer, before this is ever called.
        public void CancelByCustomer(string reason, BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
        {
            if (Status != BookingStatus.Scheduled)
                throw new BusinessRuleBrokenException("Cannot cancel an appointment that is already wrapped up or resolved.");

            if (string.IsNullOrWhiteSpace(reason))
                throw new BusinessRuleBrokenException("A cancellation reason trace parameter is required.");

            Status = BookingStatus.Cancelled;
            CancellationReason = reason.Trim();
            CancelledAt = DateTime.UtcNow;
            CancelledByUserId = null;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BookingCancelledDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CustomerId: this.CustomerId.Value,
                Reason: CancellationReason,
                CancelledByUserId: null,
                CancelledAt: CancelledAt.Value
            ));

            AddDomainEvent(new BookingCancellationNoticeDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CancelledAt: CancelledAt.Value
            ));

            RaiseRefundEligibilityIfAny(bookingPolicy, paymentPolicy, ewalletProvider, ewalletNumber, ewalletName);
        }

        // Shared by Cancel and CancelByCustomer — evaluates refund eligibility and, if eligible,
        // requires e-wallet details to already be present (they're collected up front, in the same
        // cancellation request) before raising the event that creates a RefundRequest.
        private void RaiseRefundEligibilityIfAny(BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy, string? ewalletProvider, string? ewalletNumber, string? ewalletName)
        {
            var (shouldRefund, refundAmount) = EvaluateRefund(bookingPolicy, paymentPolicy);
            if (!shouldRefund)
                return;

            if (string.IsNullOrWhiteSpace(ewalletProvider) || string.IsNullOrWhiteSpace(ewalletNumber) || string.IsNullOrWhiteSpace(ewalletName))
                throw new BusinessRuleBrokenException("E-wallet details are required to cancel a booking eligible for a refund.");

            RefundStatus = RefundStatus.Pending;
            AddDomainEvent(new BookingRefundEligibleDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                RefundAmount: refundAmount,
                CurrencyCode: this.CurrencyCode,
                EwalletProvider: ewalletProvider,
                EwalletNumber: ewalletNumber,
                EwalletName: ewalletName,
                OccurredAt: CancelledAt!.Value
            ));
        }

        // Timing-based, not actor-based — the same calculation is used whether a staff member or
        // the customer themselves triggered the cancellation. Only ever returns ShouldRefund: true
        // for bookings that were actually paid online; pay-in-visit bookings are never refunded.
        private (bool ShouldRefund, decimal Amount) EvaluateRefund(BookingPolicy? bookingPolicy, PaymentPolicy? paymentPolicy)
        {
            if (!RequiresUpfrontPayment || PaymentConfirmedVia != PaymentConfirmationMethod.Online)
                return (false, 0m);

            if (paymentPolicy?.RefundEnabled != true)
                return (false, 0m);

            var scheduledAt = ScheduledDate.ToDateTime(ScheduledStartTime);
            var cutoffHours = bookingPolicy?.CancellationCutoffHours ?? 0;
            var isOnTime = DateTime.UtcNow.AddHours(cutoffHours) <= scheduledAt;

            // On-time defaults to 100% (generous by default — a business opts INTO keeping an
            // admin fee by lowering PaymentPolicy.OnTimeRefundPercent). Late cancellation only
            // refunds anything at all if LateCancellationPolicy explicitly allows it, and
            // PartialRefund's rate is a separate, independently-configured field that defaults to
            // 0 — late cancellations hurt the business more and are penalized harder by default.
            if (isOnTime)
                return ClampRefund(AmountDue * ((paymentPolicy?.OnTimeRefundPercent ?? 100m) / 100m));

            return (bookingPolicy?.LateCancellationPolicy ?? CancellationPolicy.NoRefund) switch
            {
                CancellationPolicy.FullRefund => (true, AmountDue),
                CancellationPolicy.PartialRefund => ClampRefund(AmountDue * ((paymentPolicy?.LateCancellationRefundPercent ?? 0m) / 100m)),
                _ => (false, 0m),
            };
        }

        // A configured refund percentage of 0 means no money actually moves — treat that the same
        // as "no refund" rather than raising a zero-amount refund event, which RefundRequest.Create
        // rejects outright (amount must be > 0).
        private static (bool ShouldRefund, decimal Amount) ClampRefund(decimal amount) =>
            amount > 0 ? (true, amount) : (false, 0m);

        // Called by ConfirmRefundRequestHandler once an Owner/Admin has confirmed the manual
        // e-wallet transfer with a receipt uploaded as proof.
        public void ConfirmReviewedRefund(decimal amount, string receiptUrl)
        {
            RefundStatus = RefundStatus.Refunded;
            RefundedAmount = amount;
            RefundedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BookingRefundConfirmedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                RefundedAmount: amount,
                CurrencyCode: this.CurrencyCode,
                ReceiptUrl: receiptUrl,
                OccurredAt: DateTime.UtcNow
            ));
        }

        // Called when a refund review is rejected — raises a reliable event so the customer gets
        // told why, exactly when the decision happens.
        public void RejectReviewedRefund(string reason)
        {
            RefundStatus = RefundStatus.Rejected;
            UpdatedAt = DateTime.UtcNow;

            AddDomainEvent(new BookingRefundRejectedDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                RejectionReason: reason,
                OccurredAt: DateTime.UtcNow
            ));
        }
    }
}
