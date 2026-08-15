using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using ApexBooking.Core.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    public class SendThankYouEmailOnBookingCompletedHandler
        : INotificationHandler<DomainEventNotification<BookingCompletedDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly ILogger<SendThankYouEmailOnBookingCompletedHandler> _logger;

        public SendThankYouEmailOnBookingCompletedHandler(
            IUnitOfWork unitOfWork,
            IBookingNotificationService bookingNotificationService,
            ILogger<SendThankYouEmailOnBookingCompletedHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _bookingNotificationService = bookingNotificationService;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<BookingCompletedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            // 1. Single database load: Fetch the active tenant along with its Business Profile name.
            // Tenant does not implement ITenantEntity, so the global EF query filter doesn't cover this
            // lookup — scope explicitly by the TenantId already carried on the domain event itself
            // (more reliable here than the ambient HTTP-derived ITenantEntity, since this handler runs
            // as a domain-event side effect, not directly off an inbound request).
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: t => t.BusinessProfile!
            );

            if (tenant == null || tenant.BusinessProfile == null)
            {
                _logger.LogError("Could not resolve workspace details for Tenant {TenantId}. Thank-you email was aborted.", e.TenantId);
                return;
            }

            // 2. Fetch the concrete Service catalog entry name inside the aggregate scope boundary
            var serviceIdVo = new ServiceId(e.ServiceId);
            var serviceTenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.Services.Any(s => s.ServiceId == serviceIdVo),
                includes: t => t.Services
            );
            var service = serviceTenant?.Services.FirstOrDefault(s => s.ServiceId.Value == e.ServiceId);

            if (service == null)
            {
                _logger.LogError("Could not resolve service catalog item {ServiceId} for Tenant {TenantId}. Thank-you email was aborted.", e.ServiceId, e.TenantId);
                return;
            }

            // 3. Resolve Customer Parameters
            // (Assuming your system has a Customer aggregate or a Customer child list inside the Tenant aggregate root)
            // Replace this placeholder mapping with your real Customer data store query:
            string targetCustomerEmail = "customer@example.com"; // e.g. _unitOfWork.CustomerRepository.GetById(e.CustomerId).Email
            string customerName = "Valued Client";

            _logger.LogInformation(
                "Compiling service complete notification template for Booking reference {BookingReference} targeting {Email}.",
                e.BookingReference,
                targetCustomerEmail);

            // 4. Dispatch the rich HTML template email using your specialized infrastructure service
            await _bookingNotificationService.SendThankYouEmailAsync(
                to: targetCustomerEmail,
                customerName: customerName,
                businessName: tenant.BusinessProfile.BusinessName,
                serviceName: service.Name,
                bookingReference: e.BookingReference,
                ct: cancellationToken
            );

            _logger.LogInformation(
                "Successfully dispatched service completion thank you email for Reference {BookingReference} to {Email}.",
                e.BookingReference,
                targetCustomerEmail);
        }
    }
}