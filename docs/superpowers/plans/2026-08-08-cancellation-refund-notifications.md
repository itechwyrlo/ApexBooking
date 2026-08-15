# Cancellation & Refund-Outcome Notifications Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Notify the customer by email when their booking is cancelled (with a refund note if applicable), and notify the tenant owner in-app once a refund actually resolves.

**Architecture:** A new thin `IReliableDomainEvent` (`BookingCancellationNoticeDomainEvent`) drives a new email handler that re-reads the booking's live `RefundStatus` at send time. The existing refund handler gains a second responsibility: push an owner-facing bell notification once it knows the real outcome.

**Tech Stack:** Same as the refund-processing plan — MediatR notification handlers, the outbox/relay pipeline, `INotificationService` (Brevo) for email, `IRealtimeNotificationDispatcher` (SignalR) for the bell.

## Global Constraints

- Spec: [docs/superpowers/specs/2026-08-08-booking-cancellation-refund-notifications-design.md](../specs/2026-08-08-booking-cancellation-refund-notifications-design.md)
- No second customer email for refund outcome — see spec's Decisions.
- Per standing session instruction: implement only — no `dotnet build`/`test`/`ef migrations`/`git commit` per task; the user runs those manually. (This plan adds no new persisted columns, so no migration is needed this time.)

---

### Task 1: `BookingCancellationNoticeDomainEvent` + `NotificationEventType` additions

**Files:**
- Modify: `ApexBooking.Core.Domain/Events/BookingEvents.cs`
- Modify: `ApexBooking.Core.Domain/Enums/NotificationEventType.cs`

**Interfaces:**
- Produces: `record BookingCancellationNoticeDomainEvent(TenantId TenantId, Guid BookingId, string BookingReference, DateTime CancelledAt) : IReliableDomainEvent`
- Produces: `NotificationEventType.RefundSucceeded`, `NotificationEventType.RefundFailed`

- [ ] **Step 1: Append the event to `BookingEvents.cs`**, after `BookingRefundDueDomainEvent`:

```csharp
// Drives the customer-facing cancellation email — raised unconditionally alongside
// BookingCancelledDomainEvent (regardless of refund eligibility). Deliberately thin: its
// handler re-loads the Booking fresh and reads whatever RefundStatus is current at send
// time, since this event and BookingRefundDueDomainEvent land in the outbox in the same
// commit and can be relayed in either order.
public record BookingCancellationNoticeDomainEvent(
    TenantId TenantId,
    Guid BookingId,
    string BookingReference,
    DateTime CancelledAt
) : IReliableDomainEvent;
```

- [ ] **Step 2: Add the two enum values** to `NotificationEventType.cs`, in the "Tenant Admin" section:

```csharp
    BookingPendingPayment,
    PaymentCaptured,
    RefundSucceeded,
    RefundFailed,
    StaffCreated,
```

---

### Task 2: Raise the new event from `Booking.Cancel`/`CancelByCustomer`

**Files:**
- Modify: `ApexBooking.Core.Domain/Entities/Booking.cs`

**Interfaces:**
- Consumes: `BookingCancellationNoticeDomainEvent` (Task 1).

- [ ] **Step 1:** In `Cancel(...)`, immediately after the existing `AddDomainEvent(new BookingCancelledDomainEvent(...))` call (and before the `EvaluateRefund` block already there from the refund-processing plan), add:

```csharp
            AddDomainEvent(new BookingCancellationNoticeDomainEvent(
                TenantId: this.TenantId,
                BookingId: this.BookingId.Value,
                BookingReference: this.BookingReference,
                CancelledAt: CancelledAt.Value
            ));
```

- [ ] **Step 2:** Make the identical addition in `CancelByCustomer(...)`, same position relative to its own `BookingCancelledDomainEvent`/`EvaluateRefund` block.

---

### Task 3: `IBookingNotificationService.SendBookingCancellationEmailAsync`

**Files:**
- Modify: `ApexBooking.Core.Domain/Services/Notification/Bookings/IBookingNotificationService.cs`
- Modify: `ApexBooking.Infrastructure/ExternalServices/BookingNotificationService/BookingNotificationService.cs`

**Interfaces:**
- Produces: `Task SendBookingCancellationEmailAsync(string to, string customerName, string businessName, string serviceName, string bookingReference, string? refundNote, CancellationToken ct)`

- [ ] **Step 1:** Add to the interface, after `SendBookingConfirmationEmailAsync`:

```csharp
        Task SendBookingCancellationEmailAsync(
            string to,
            string customerName,
            string businessName,
            string serviceName,
            string bookingReference,
            string? refundNote,
            CancellationToken ct);
```

- [ ] **Step 2:** Implement in `BookingNotificationService.cs`, after `SendBookingConfirmationEmailAsync`, matching the existing two templates' visual style:

```csharp
        public Task SendBookingCancellationEmailAsync(
            string to,
            string customerName,
            string businessName,
            string serviceName,
            string bookingReference,
            string? refundNote,
            CancellationToken ct)
        {
            var refundBlock = string.IsNullOrWhiteSpace(refundNote)
                ? string.Empty
                : $@"
                <div style='background: #f8f9fa; border-left: 4px solid #0d6efd; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                    <p style='margin: 0;'>{refundNote}</p>
                </div>";

            var body = $@"
            <div style='font-family: Arial, sans-serif; line-height: 1.6; color: #333; max-width: 600px; margin: 0 auto; border: 1px solid #f0f0f0; padding: 20px; border-radius: 8px;'>
                <h2 style='color: #2c3e50; border-bottom: 2px solid #d33; padding-bottom: 10px;'>Your Appointment was Cancelled</h2>

                <p>Hi <strong>{customerName}</strong>,</p>

                <p>Your <strong>{serviceName}</strong> appointment with <strong>{businessName}</strong> has been cancelled.</p>

                <div style='background: #f8f9fa; border-left: 4px solid #d33; padding: 15px; margin: 20px 0; border-radius: 4px;'>
                    <p style='margin: 0; font-size: 14px; color: #555;'><strong>Appointment Tracking Reference:</strong></p>
                    <p style='margin: 5px 0 0 0; font-size: 18px; font-weight: bold; color: #d33; letter-spacing: 1px;'>{bookingReference}</p>
                </div>
                {refundBlock}
                <p>If you'd like to book again, feel free to visit our online booking page any time.</p>

                <p style='margin-top: 30px; font-size: 14px; color: #777;'>
                    Best regards,<br>
                    The Team at <strong>{businessName}</strong>
                </p>
                <hr style='border: 0; border-top: 1px solid #eef0f1; margin: 20px 0;'>
                <p style='font-size: 11px; color: #aaa; text-align: center; margin: 0;'>This is an automated operational notification receipt. Please do not reply directly to this email address.</p>
            </div>";

            return _notification.SendEmailAsync(
                to: to,
                subject: $"Appointment Cancelled — {businessName}",
                content: body
            );
        }
```

---

### Task 4: `SendBookingCancellationEmailHandler` (new)

**Files:**
- Create: `ApexBooking.Core.Application/Features/Bookings/Events/SendBookingCancellationEmailHandler.cs`

**Interfaces:**
- Consumes: `BookingCancellationNoticeDomainEvent` (Task 1), `IBookingNotificationService.SendBookingCancellationEmailAsync` (Task 3).

- [ ] **Step 1: Write the handler**

```csharp
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using ApexBooking.Core.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    public class SendBookingCancellationEmailHandler
        : INotificationHandler<DomainEventNotification<BookingCancellationNoticeDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly ILogger<SendBookingCancellationEmailHandler> _logger;

        public SendBookingCancellationEmailHandler(
            IUnitOfWork unitOfWork,
            IBookingNotificationService bookingNotificationService,
            ILogger<SendBookingCancellationEmailHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _bookingNotificationService = bookingNotificationService;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<BookingCancellationNoticeDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.BusinessProfile!, t => t.Bookings, t => t.Services]
            );

            if (tenant == null || tenant.BusinessProfile == null)
            {
                _logger.LogError("Could not resolve workspace details for Tenant {TenantId}. Cancellation email was aborted.", e.TenantId);
                return;
            }

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == e.BookingId);
            if (booking == null)
            {
                _logger.LogError("Could not resolve Booking {BookingId} for Tenant {TenantId}. Cancellation email was aborted.", e.BookingId, e.TenantId);
                return;
            }

            var service = tenant.Services.FirstOrDefault(s => s.ServiceId == booking.ServiceId);
            var serviceName = service?.Name ?? "your service";

            var customer = await _unitOfWork.CustomerRepository.GetAsync(predicate: c => c.CustomerId == booking.CustomerId);
            if (customer?.Contact.Email is not { } customerEmail)
            {
                _logger.LogWarning("Customer {CustomerId} has no email on file. Cancellation email for {BookingReference} was skipped.", booking.CustomerId.Value, e.BookingReference);
                return;
            }

            // Reads whatever RefundStatus is current right now — may already reflect a resolved
            // outcome if ProcessRefundOnBookingCancelledHandler's outbox message was relayed first,
            // or may still show Pending if this one won the race. Both are accurate to report.
            string? refundNote = booking.RefundStatus switch
            {
                RefundStatus.Pending or RefundStatus.Processing =>
                    $"A refund of {booking.RefundedAmount ?? booking.AmountDue:0.00} {booking.CurrencyCode} is being processed and should reflect in your account within a few business days.",
                RefundStatus.Succeeded =>
                    $"A refund of {booking.RefundedAmount:0.00} {booking.CurrencyCode} has been processed.",
                _ => null, // Failed: not the customer's problem to see in an automated email. None: nothing to say.
            };

            await _bookingNotificationService.SendBookingCancellationEmailAsync(
                to: customerEmail,
                customerName: customer.Contact.Name,
                businessName: tenant.BusinessProfile.BusinessName,
                serviceName: serviceName,
                bookingReference: e.BookingReference,
                refundNote: refundNote,
                ct: cancellationToken
            );

            _logger.LogInformation(
                "Successfully dispatched booking cancellation email for Reference {BookingReference} to {Email}.",
                e.BookingReference,
                customerEmail);
        }
    }
}
```

---

### Task 5: Refund-outcome bell notification

**Files:**
- Modify: `ApexBooking.Core.Application/Features/Bookings/Events/ProcessRefundOnBookingCancelledHandler.cs`

**Interfaces:**
- Consumes: `IRealtimeNotificationDispatcher` (existing, same one `NotifyTenantOnBookingCancelledHandler.cs` uses), `Notification.Create` (existing), `NotificationEventType.RefundSucceeded`/`RefundFailed` (Task 1).

- [ ] **Step 1:** Add the dependency and constructor param:

```csharp
using ApexBooking.Core.Application.Common.Notifications;
using ApexBooking.Core.Domain.Entities;
```

```csharp
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayMongoService _payMongoService;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<ProcessRefundOnBookingCancelledHandler> _logger;

        public ProcessRefundOnBookingCancelledHandler(
            IUnitOfWork unitOfWork,
            IPayMongoService payMongoService,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<ProcessRefundOnBookingCancelledHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _payMongoService = payMongoService;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }
```

- [ ] **Step 2:** Add `t => t.Members` to the existing tenant load:

```csharp
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.Bookings, t => t.PaymentCredential!, t => t.Members]);
```

- [ ] **Step 3:** Replace the method's tail — from the `try` block through the final `CompleteAsync` — with a version that also creates and pushes the owner notification on both branches:

```csharp
            Notification? outcomeNotification = null;

            try
            {
                var result = await _payMongoService.CreateRefundAsync(
                    tenantSecretKey: secretKey,
                    payMongoPaymentId: payMongoPaymentId,
                    amountPhp: e.RefundAmount,
                    reason: "requested_by_customer",
                    cancellationToken: cancellationToken);

                booking.RecordRefundOutcome(result.Status, e.RefundAmount);

                _logger.LogInformation(
                    "PayMongo refund {RefundId} for Booking {BookingReference} resolved with status {Status}.",
                    result.RefundId, e.BookingReference, result.Status);

                outcomeNotification = BuildOutcomeNotification(tenant, e, succeeded: true);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(
                    ex,
                    "PayMongo refund call failed for Booking {BookingReference}. Cancellation itself already committed and stays in effect.",
                    e.BookingReference);
                booking.RecordRefundOutcome(RefundStatus.Failed, null);

                outcomeNotification = BuildOutcomeNotification(tenant, e, succeeded: false);
            }

            if (outcomeNotification != null)
                _unitOfWork.NotificationRepository.Add(outcomeNotification);

            _unitOfWork.TenantRepository.Update(tenant);
            await _unitOfWork.CompleteAsync(cancellationToken);

            if (outcomeNotification != null)
                await _realtimeDispatcher.PushAsync([outcomeNotification], cancellationToken);
        }

        // Returns null (skips the notification, doesn't fail the refund) if no Owner can be
        // resolved — same graceful-skip posture NotifyTenantOnBookingCancelledHandler already uses.
        private static Notification? BuildOutcomeNotification(Domain.Entities.Tenant tenant, BookingRefundDueDomainEvent e, bool succeeded)
        {
            var owner = tenant.Members.FirstOrDefault(m => m.Role == Domain.Enums.SystemRole.Owner);
            if (owner?.UserId is not { } ownerUserId)
                return null;

            return succeeded
                ? Notification.Create(
                    ownerUserId,
                    Domain.Enums.NotificationRecipientType.TenantAdmin,
                    e.TenantId,
                    NotificationEventType.RefundSucceeded,
                    "Refund Processed",
                    $"A refund of {e.RefundAmount:0.00} {e.CurrencyCode} was processed for Booking {e.BookingReference}.")
                : Notification.Create(
                    ownerUserId,
                    Domain.Enums.NotificationRecipientType.TenantAdmin,
                    e.TenantId,
                    NotificationEventType.RefundFailed,
                    "Refund Failed",
                    $"The refund for Booking {e.BookingReference} could not be processed automatically. Please review it in PayMongo directly.");
        }
    }
}
```

Note: this replaces everything from the original handler's `try` block down to its closing braces — the `BuildOutcomeNotification` helper is a new private static method added to the same class, and its parameter/return types use fully-qualified `Domain.Entities.Tenant`/`Domain.Enums.SystemRole`/`Domain.Enums.NotificationRecipientType` to avoid a naming collision with `ApexBooking.Core.Domain.Entities.Notification` already `using`'d at the top — alternatively, just add `using ApexBooking.Core.Domain.Entities;` and `using ApexBooking.Core.Domain.Enums;` (both are likely already partially present — check the file's existing usings first and reconcile rather than duplicating) and drop the fully-qualified prefixes for a cleaner read.

---

## Self-Review Notes

- **Spec coverage:** customer email (Task 3+4), refund-note-reads-live-status (Task 4's switch expression), owner outcome bell for both success and failure (Task 5) — every Decisions/Design bullet has a task.
- **Type consistency:** `NotificationEventType.RefundSucceeded`/`RefundFailed` (Task 1) used identically in Task 5. `BookingCancellationNoticeDomainEvent`'s fields (Task 1) match exactly what Task 4's handler reads off it.
- **Ambiguity resolved:** Task 5's note about `using` reconciliation is flagged explicitly rather than silently assumed, since the plan was written without re-opening `ProcessRefundOnBookingCancelledHandler.cs`'s current top-of-file usings at plan-writing time — check them before pasting.
