using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.TenantRequest.Events
{

    public class SendOwnerSetupInvitationOnTenantCreatedHandler
        : INotificationHandler<DomainEventNotification<TenantCreatedDomainEvent>>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IAuthNotificationService _authNotificationService;
        private readonly IAppUrlService _appUrlService;
        private readonly ILogger<SendOwnerSetupInvitationOnTenantCreatedHandler> _logger;

        public SendOwnerSetupInvitationOnTenantCreatedHandler(
            IApplicationUserService applicationUserService,
            IAuthNotificationService authNotificationService,
            IAppUrlService appUrlService,
            ILogger<SendOwnerSetupInvitationOnTenantCreatedHandler> logger)
        {
            _applicationUserService = applicationUserService;
            _authNotificationService = authNotificationService;
            _appUrlService = appUrlService;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<TenantCreatedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var ticket = await _applicationUserService.GeneratePasswordResetTokenAsync(e.OwnerEmail);

            if (ticket is null || string.IsNullOrWhiteSpace(ticket.Token))
            {
                _logger.LogError(
                    "Could not mint a setup token for owner {OwnerEmail} of tenant {TenantId}; the invitation email was not sent.",
                    e.OwnerEmail,
                    e.TenantId);
                return;
            }

        
            var setupUrl = _appUrlService.GetPasswordResetUrl(ticket.UserId.ToString(), ticket.Token, e.Slug);

            await _authNotificationService.SendInvitationEmailAsync(
                e.OwnerEmail,
                $"{e.OwnerFirstName} {e.OwnerLastName}".Trim(),
                e.BusinessName,
                SystemRole.Owner.ToString(),
                setupUrl,
                cancellationToken);
        }
    }
}
