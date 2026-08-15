using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Application.Common.Notifications;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Tenancy.Events
{
    // Listens on the same TeamMemberInvitedDomainEvent that drives
    // SendStaffSetupInvitationOnTeamMemberInvitedHandler (which emails the invitee) — this one
    // notifies the Owner that a new team member was added.
    public class NotifyTenantOnStaffCreatedHandler
        : INotificationHandler<DomainEventNotification<TeamMemberInvitedDomainEvent>>
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IRealtimeNotificationDispatcher _realtimeDispatcher;
        private readonly ILogger<NotifyTenantOnStaffCreatedHandler> _logger;

        public NotifyTenantOnStaffCreatedHandler(
            IUnitOfWork unitOfWork,
            IRealtimeNotificationDispatcher realtimeDispatcher,
            ILogger<NotifyTenantOnStaffCreatedHandler> logger)
        {
            _unitOfWork = unitOfWork;
            _realtimeDispatcher = realtimeDispatcher;
            _logger = logger;
        }

        public async Task Handle(DomainEventNotification<TeamMemberInvitedDomainEvent> notification, CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var tenant = await _unitOfWork.TenantRepository.GetAsync(
                predicate: t => t.TenantId == e.TenantId,
                includes: [t => t.Members]);

            var owner = tenant?.Members.FirstOrDefault(m => m.Role == SystemRole.Owner);
            if (owner?.UserId is not { } ownerUserId)
            {
                _logger.LogWarning(
                    "Could not resolve an Owner for Tenant {TenantId}; staff-created notification skipped for {Email}.",
                    e.TenantId, e.Email);
                return;
            }

            // The invitee themselves might be the Owner reading this (unlikely, but harmless) —
            // skip self-notifying, they already get the setup invitation email.
            if (owner.UserId == e.ApplicationUserId)
                return;

            var n = Notification.Create(
                ownerUserId,
                NotificationRecipientType.TenantAdmin,
                e.TenantId,
                NotificationEventType.StaffCreated,
                "New Team Member",
                $"{e.FullName} was added to your team as {e.AssignedRole}.");
            _unitOfWork.NotificationRepository.Add(n);

            await _unitOfWork.CompleteAsync(cancellationToken);
            await _realtimeDispatcher.PushAsync([n], cancellationToken);
        }
    }
}
