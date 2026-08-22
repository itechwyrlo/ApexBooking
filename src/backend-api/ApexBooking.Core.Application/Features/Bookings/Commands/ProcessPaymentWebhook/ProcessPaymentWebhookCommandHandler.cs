using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Paymongo;
using ApexBooking.SharedKernel.Exceptions;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ProcessPaymentWebhook
{
    public class ProcessPaymentWebhookCommandHandler : ICommandHandler<ProcessPaymentWebhookCommand>
    {
        private static readonly TimeSpan MaxSignatureAge = TimeSpan.FromMinutes(5);

        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayMongoWebhookSignatureVerifier _signatureVerifier;
        private readonly IProcessedPaymentEventStore _processedPaymentEventStore;
        private readonly ILogger<ProcessPaymentWebhookCommandHandler> _logger;

        public ProcessPaymentWebhookCommandHandler(
            IUnitOfWork unitOfWork,
            IPayMongoWebhookSignatureVerifier signatureVerifier,
            IProcessedPaymentEventStore processedPaymentEventStore,
            ILogger<ProcessPaymentWebhookCommandHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _signatureVerifier = signatureVerifier;
            _processedPaymentEventStore = processedPaymentEventStore;
            _logger = logger;
        }

        public async Task Handle(ProcessPaymentWebhookCommand command, CancellationToken cancellationToken)
        {
            // 1. Invariant Guard Check: Verify this webhook resource belongs to our booking system format
            if (string.IsNullOrWhiteSpace(command.RemarksToken) || !command.RemarksToken.StartsWith("BOOKING_"))
                return; // Soft ignore if it's an unrelated payment resource or system event trace

            // 2. Extract the primitive C# Guid out from our custom string tracker.
            // Accepts both "BOOKING_{bookingId}" (legacy) and "BOOKING_{bookingId}_{branchId}" —
            // the branch segment, when present, is informational only; the booking row is authoritative.
            var segments = command.RemarksToken["BOOKING_".Length..].Split('_', StringSplitOptions.RemoveEmptyEntries);
            if (segments.Length == 0 || !Guid.TryParse(segments[0], out Guid targetBookingId))
                throw new BusinessRuleBrokenException("Invalid tracking token structure detected inside incoming PayMongo webhook metadata.");

            // 3. Single Database Load: resolve the owning tenant from the booking id itself — never
            // trust a tenant id supplied directly by the payload.
            var tenant = await _unitOfWork.TenantRepository.GetByBookingIdAsync(targetBookingId, cancellationToken);

            if (tenant == null)
                throw new BusinessRuleBrokenException("Payment confirmation failed. Target appointment record context could not be located inside our system database ledgers.");

            // 3b. Reject anything that isn't actually signed by this tenant's own PayMongo webhook
            // secret — up to this point, RemarksToken/RawBody are attacker-controlled input; nothing
            // above is safe to act on without this check passing first.
            if (tenant.PaymentCredential?.WebhookSecret is not { } webhookSecret)
                throw new BusinessRuleBrokenException("Payment confirmation failed. No webhook signing secret is configured for this business workspace.");

            var isLiveMode = tenant.PaymentCredential.SecretKey.StartsWith("sk_live_");
            if (!_signatureVerifier.Verify(command.RawBody, command.SignatureHeader, webhookSecret, isLiveMode))
                throw new BusinessRuleBrokenException("Payment confirmation failed. Webhook signature verification failed.");

            // 3c. Reject stale deliveries — a replayed or clock-skewed signature older than 5
            // minutes is rejected even though it's cryptographically valid. Checked after Verify()
            // (not before) because the timestamp only means anything once we know the signature
            // wasn't forged.
            if (!_signatureVerifier.TryGetTimestamp(command.SignatureHeader, out var signedAt) ||
                DateTimeOffset.UtcNow - signedAt > MaxSignatureAge)
                throw new BusinessRuleBrokenException("Payment confirmation failed. Webhook signature timestamp is missing or too old.");

            // 4. Idempotency check — a redelivery of an event we've already processed is a clean
            // no-op, not an error: log and return successfully so the controller keeps returning
            // HTTP 200 and PayMongo stops retrying.
            if (string.IsNullOrWhiteSpace(command.PayMongoEventId))
                throw new BusinessRuleBrokenException("Payment confirmation failed. Missing PayMongo event id.");

            if (await _processedPaymentEventStore.ExistsAsync(command.PayMongoEventId, cancellationToken))
            {
                _logger.LogInformation(
                    "PayMongo event {PayMongoEventId} for booking {BookingId} was already processed; skipping.",
                    command.PayMongoEventId, targetBookingId);
                return;
            }

            // 5. Extract the child Booking entity node out from the parent aggregate graph tree
            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == targetBookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment details missing inside parent aggregate boundary graph lines.");

            // 5b. Amount verification — the signature only proves PayMongo sent this payload, not
            // that it says what we expect. A stale Link, a dashboard edit on PayMongo's side, or an
            // upstream pricing bug could all produce a validly-signed "paid" event for the wrong
            // amount; without this check the booking would silently confirm regardless. Compared in
            // centavos (PayMongo's own unit) to avoid decimal-vs-integer rounding mismatches.
            var expectedCentavos = (long)Math.Round(booking.AmountDue * 100m, MidpointRounding.AwayFromZero);
            if (command.PaidAmountInCentavos is not { } paidCentavos || paidCentavos != expectedCentavos)
                throw new BusinessRuleBrokenException(
                    $"Payment confirmation failed. Paid amount did not match the amount due for booking {targetBookingId}.");

            // 6. Invoke the domain state machine — PendingPayment -> Scheduled.
            booking.ConfirmPayment(PaymentConfirmationMethod.Online, command.PayMongoPaymentId);

            // 7. Track the ledger row on the SAME DbContext, WITHOUT saving yet (see
            // IProcessedPaymentEventStore.Add's doc comment) — it must commit in the same
            // transaction as the booking status change, never before it.
            _processedPaymentEventStore.Add(ProcessedPaymentEvent.Create(
                command.PayMongoEventId, tenant.TenantId.Value, booking.BookingId.Value));

            _unitOfWork.TenantRepository.Update(tenant);

            // 8. One SaveChangesAsync call commits the booking's payment confirmation and the
            // idempotency ledger row atomically. TryCompleteAsync (rather than CompleteAsync)
            // tolerates the TOCTOU race where a concurrent duplicate delivery of the same event
            // also passed the ExistsAsync check above and is racing to insert the same
            // PayMongoEventId — only one write can win the ledger's unique index; the loser lands
            // here having ALSO already applied ConfirmPayment() to its in-memory booking, but that
            // write never lands (the whole SaveChanges batch rolls back together), so it's a clean
            // no-op, not a real failure.
            if (!await _unitOfWork.TryCompleteAsync(cancellationToken))
            {
                _logger.LogInformation(
                    "PayMongo event {PayMongoEventId} for booking {BookingId} lost a concurrent-delivery race against an in-flight duplicate; treating as already processed.",
                    command.PayMongoEventId, targetBookingId);
            }
        }
    }
}
