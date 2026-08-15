using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Application.Common.Notifications;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Bookings.Events
{
    public class NotifyTenantOnBookingNoShowHandler
        : INotificationHandler<DomainEventNotification<BookingNoShowDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<NotifyTenantOnBookingNoShowHandler> _logger;

        public NotifyTenantOnBookingNoShowHandler(
            IUnitOfWork unitOfWork,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<NotifyTenantOnBookingNoShowHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<BookingNoShowDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.Members]);

            var owner = tenant?.Members.FirstOrDefault(m => m.Role == SystemRole.Owner);
            if (owner?.UserId is not { } ownerUserId)
            {
                _logger.LogWarning(
                    "Could not resolve an Owner for Tenant {TenantId}; booking-no-show notification skipped for {BookingReference}.",
                    e.TenantId, e.BookingReference);
                return;
            }

            var n = Notification.Create(
                ownerUserId,
                NotificationRecipientType.TenantAdmin,
                e.TenantId,
                NotificationEventType.BookingNoShow,
                "Customer No-Show",
                $"The customer did not show up for booking {e.BookingReference}.");
            _unitOfWork.NotificationRepository.Add(n);

            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync([n], cancellationToken);
        }
    }
}
