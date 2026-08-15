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
    public class NotifyTenantOnBookingCreatedHandler
        : INotificationHandler<DomainEventNotification<BookingCreatedDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<NotifyTenantOnBookingCreatedHandler> _logger;

        public NotifyTenantOnBookingCreatedHandler(
            IUnitOfWork unitOfWork,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<NotifyTenantOnBookingCreatedHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<BookingCreatedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.Members]);

            var owner = tenant?.Members.FirstOrDefault(m => m.Role == SystemRole.Owner);
            if (owner?.UserId is not { } ownerUserId)
            {
                _logger.LogWarning(
                    "Could not resolve an Owner for Tenant {TenantId}; booking-created notification skipped for {BookingReference}.",
                    e.TenantId, e.BookingReference);
                return;
            }

            var n = Notification.Create(
                ownerUserId,
                NotificationRecipientType.TenantAdmin,
                e.TenantId,
                NotificationEventType.BookingCreated,
                "New Booking",
                $"A new booking ({e.BookingReference}) was just created.");
            _unitOfWork.NotificationRepository.Add(n);

            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync([n], cancellationToken);
        }
    }
}
