using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Paymongo;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ProcessPaymentWebhook
{
    public class ProcessPaymentWebhookCommandHandler : ICommandHandler<ProcessPaymentWebhookCommand>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IPayMongoWebhookSignatureVerifier _signatureVerifier;

        public ProcessPaymentWebhookCommandHandler(IUnitOfWork unitOfWork, IPayMongoWebhookSignatureVerifier signatureVerifier)
        {
            _unitOfWork = unitOfWork;
            _signatureVerifier = signatureVerifier;
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

            // 3. Single Database Load: Extract the isolated tenant block containing the specific booking log.
            // Also hydrates PaymentCredential — needed below to verify this request actually came
            // from PayMongo before trusting it. Uses GetByBookingIdAsync (not the generic GetAsync)
            // because a t.Bookings.Any(...) predicate can't be translated for TenantId's converted type.
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

            // 4. Extract the child Booking entity node out from the parent aggregate graph tree
            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == targetBookingId);
            if (booking == null)
                throw new BusinessRuleBrokenException("Target appointment details missing inside parent aggregate boundary graph lines.");

            // 5. Invoke the updated Domain State machine method!
            // This switches the status from PendingPayment to Scheduled and automatically raises your BookingScheduledDomainEvent!
            booking.ConfirmPayment(PaymentConfirmationMethod.Online, command.PayMongoPaymentId);

            // 6. Track modifications and commit atomically out to the persistent storage layer
            _unitOfWork.TenantRepository.Update(tenant);

            // 🌟 AUTOMATED INTERCEPTOR EVENT DISPATCH LINE:
            // Because you don't use outbox, executing CompleteAsync right here immediately runs your 
            // SendBookingConfirmationEmailHandler pipeline on-the-spot before sealing the transaction!
            await _unitOfWork.CompleteAsync(cancellationToken);
        }
    }
}