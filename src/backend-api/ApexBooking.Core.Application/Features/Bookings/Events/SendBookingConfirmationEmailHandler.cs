using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services.Notification.Bookings;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.Core.Domain.ValueObjects;
using MediatR;
using Microsoft.Extensions.Logging;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    public class SendBookingConfirmationEmailHandler
        : INotificationHandler<DomainEventNotification<BookingScheduledDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IBookingNotificationService _bookingNotificationService;
        private readonly ITicketTokenService _ticketTokenService;
        private readonly ICancellationTokenService _cancellationTokenService;
        private readonly IAppUrlService _appUrlService;
        private readonly ILogger<SendBookingConfirmationEmailHandler> _logger;

        public SendBookingConfirmationEmailHandler(
            IUnitOfWork unitOfWork,
            IBookingNotificationService bookingNotificationService,
            ITicketTokenService ticketTokenService,
            ICancellationTokenService cancellationTokenService,
            IAppUrlService appUrlService,
            ILogger<SendBookingConfirmationEmailHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _bookingNotificationService = bookingNotificationService;
            _ticketTokenService = ticketTokenService;
            _cancellationTokenService = cancellationTokenService;
            _appUrlService = appUrlService;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<BookingScheduledDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            // Single database load: BusinessProfile (name) + Services (service name) + Members
            // (staff name) all scoped by the TenantId already carried on the domain event — more
            // reliable here than the ambient HTTP-derived ITenantEntity, since this handler runs as
            // a domain-event side effect (sometimes off an anonymous webhook request with no ambient
            // tenant at all). A single direct "TenantId ==" predicate, not a navigation-correlation
            // predicate, so it stays translatable — see TenantRepository.GetByBookingIdAsync for why
            // that distinction matters.
            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.BusinessProfile!, t => t.Services, t => t.Members]
            );

            if (tenant == null || tenant.BusinessProfile == null)
            {
                _logger.LogError("Could not resolve workspace details for Tenant {TenantId}. Booking confirmation email was aborted.", e.TenantId);
                return;
            }

            var service = tenant.Services.FirstOrDefault(s => s.ServiceId.Value == e.ServiceId);
            if (service == null)
            {
                _logger.LogError("Could not resolve service catalog item {ServiceId} for Tenant {TenantId}. Booking confirmation email was aborted.", e.ServiceId, e.TenantId);
                return;
            }

            var staff = tenant.Members.FirstOrDefault(m => m.TenantMemberId.Value == e.StaffId);
            var staffName = staff is not null ? $"{staff.FirstName} {staff.LastName}".Trim() : "your assigned staff member";

            var customer = await _unitOfWork.CustomerRepository.GetAsync(predicate: c => c.CustomerId == new CustomerId(e.CustomerId));
            if (customer?.Contact.Email is not { } customerEmail)
            {
                _logger.LogWarning("Customer {CustomerId} has no email on file. Booking confirmation email for {BookingReference} was skipped.", e.CustomerId, e.BookingReference);
                return;
            }

            // Regenerates the exact same boarding-pass token the customer already saw on the
            // success screen — HmacTicketTokenService.Issue is a pure, deterministic function of
            // these three IDs (no timestamp/nonce), so this is guaranteed to match, no extra
            // storage or event field needed beyond the BranchId already carried above. Linked as a
            // hosted image URL, not embedded — Brevo's API has no inline/CID image support, and
            // embedding it as a data: URI gets silently stripped by most email clients regardless.
            var ticketToken = _ticketTokenService.Issue(new TicketPayload(new BookingId(e.BookingId), e.TenantId, new BranchId(e.BranchId)));
            var qrImageUrl = _appUrlService.GetBookingQrImageUrl(ticketToken);

            // Independent, deterministic the same way — regenerable here with no storage, but
            // signed with its own key so this can never be replayed as an admission scan.
            var cancelToken = _cancellationTokenService.Issue(new CancellationTokenPayload(new BookingId(e.BookingId), e.TenantId));
            var cancelBookingUrl = _appUrlService.GetCustomerCancellationUrl(tenant.Slug ?? string.Empty, cancelToken);

            await _bookingNotificationService.SendBookingConfirmationEmailAsync(
                to: customerEmail,
                customerName: customer.Contact.Name,
                businessName: tenant.BusinessProfile.BusinessName,
                serviceName: service.Name,
                staffName: staffName,
                bookingReference: e.BookingReference,
                scheduledDate: e.ScheduledDate,
                scheduledStartTime: e.ScheduledStartTime,
                qrImageUrl: qrImageUrl,
                cancelBookingUrl: cancelBookingUrl,
                ct: cancellationToken
            );

            _logger.LogInformation(
                "Successfully dispatched booking confirmation email for Reference {BookingReference} to {Email}.",
                e.BookingReference,
                customerEmail);
        }
    }
}
