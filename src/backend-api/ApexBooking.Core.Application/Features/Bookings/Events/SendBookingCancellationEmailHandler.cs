using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using MediatR;
using Microsoft.Extensions.Logging;

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

            // The actual outcome (refunded/rejected) is now always covered by a dedicated,
            // reliable email fired exactly when the decision happens — SendRefundConfirmationEmailHandler
            // / SendRefundRejectionEmailHandler. This note is deliberately static; it never has to
            // guess at a RefundStatus that might still be mid-flight.
            string? refundNote = booking.RefundStatus == RefundStatus.Pending
                ? "Your refund is being reviewed — we'll email you once it's decided."
                : null;

            await _bookingNotificationService.SendBookingCancellationEmailAsync(
                to: customerEmail,
                customerName: customer.Contact.Name,
                businessName: tenant.BusinessProfile.BusinessName,
                serviceName: serviceName,
                bookingReference: e.BookingReference,
                refundNote: refundNote,
                refundStatusUrl: null,
                ct: cancellationToken
            );

            _logger.LogInformation(
                "Successfully dispatched booking cancellation email for Reference {BookingReference} to {Email}.",
                e.BookingReference,
                customerEmail);
        }
    }
}
