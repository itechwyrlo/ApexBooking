using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    public class SendRefundRejectionEmailHandler
        : INotificationHandler<DomainEventNotification<BookingRefundRejectedDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly ILogger<SendRefundRejectionEmailHandler> _logger;

        public SendRefundRejectionEmailHandler(
            IUnitOfWork unitOfWork,
            IBookingNotificationService bookingNotificationService,
            ILogger<SendRefundRejectionEmailHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _bookingNotificationService = bookingNotificationService;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<BookingRefundRejectedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.BusinessProfile!, t => t.Bookings]);

            if (tenant?.BusinessProfile is null)
            {
                _logger.LogError("Could not resolve workspace details for Tenant {TenantId}. Refund rejection email was aborted.", e.TenantId);
                return;
            }

            var booking = tenant.Bookings.FirstOrDefault(b => b.BookingId.Value == e.BookingId);
            if (booking is null)
                return;

            var customer = await _unitOfWork.CustomerRepository.GetAsync(predicate: c => c.CustomerId == booking.CustomerId);
            if (customer?.Contact.Email is not { } customerEmail)
            {
                _logger.LogWarning("Customer {CustomerId} has no email on file. Refund rejection email for {BookingReference} was skipped.", booking.CustomerId.Value, e.BookingReference);
                return;
            }

            await _bookingNotificationService.SendRefundRejectionEmailAsync(
                to: customerEmail,
                customerName: customer.Contact.Name,
                businessName: tenant.BusinessProfile.BusinessName,
                bookingReference: e.BookingReference,
                rejectionReason: e.RejectionReason,
                ct: cancellationToken
            );
        }
    }
}
