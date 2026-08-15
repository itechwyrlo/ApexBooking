using ApexBooking.Core.Application.Common.DomainEvent;
using ApexBooking.Core.Domain.Events;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.Services;
using ApexBooking.Core.Domain.Services.Notification.Auth;
using MediatR;
using Microsoft.Extensions.Logging;

namespace ApexBooking.Core.Application.Features.Tenancy.Events
{
   public class SendStaffSetupInvitationOnTeamMemberInvitedHandler
        : INotificationHandler<DomainEventNotification<TeamMemberInvitedDomainEvent>>
    {
        private readonly IApplicationUserService _applicationUserService;
        private readonly IAuthNotificationService _authNotificationService;
        private readonly IAppUrlService _appUrlService;
        private readonly ILogger<SendStaffSetupInvitationOnTeamMemberInvitedHandler> _logger;

        public SendStaffSetupInvitationOnTeamMemberInvitedHandler(
            IApplicationUserService applicationUserService,
            IAuthNotificationService authNotificationService,
            IAppUrlService appUrlService,
            ILogger<SendStaffSetupInvitationOnTeamMemberInvitedHandler> logger)
        {
            _applicationUserService = applicationUserService;
            _authNotificationService = authNotificationService;
            _appUrlService = appUrlService;
            _logger = logger;
        }

        public async Task Handle(
            DomainEventNotification<TeamMemberInvitedDomainEvent> notification,
            CancellationToken cancellationToken)
        {
            var e = notification.DomainEvent;

            var ticket = await _applicationUserService.GeneratePasswordResetTokenAsync(e.Email);

            if (ticket is null || string.IsNullOrWhiteSpace(ticket.Token))
            {
                _logger.LogError(
                    "Could not mint a setup token for invited team member {Email} of tenant {TenantId}; the invitation email was not sent.",
                    e.Email,
                    e.TenantId);
                return;
            }

            var setupUrl = _appUrlService.GetPasswordResetUrl(ticket.UserId.ToString(), ticket.Token, e.Slug);

            await _authNotificationService.SendInvitationEmailAsync(
                e.Email,
                e.FullName,
                "Your Team Workspace",
                e.AssignedRole.ToString(),
                setupUrl,
                cancellationToken);
                
            _logger.LogInformation(
                "Successfully dispatched team invitation email to {Email} with role {Role} for Tenant {TenantId}.",
                e.Email,
                e.AssignedRole,
                e.TenantId);
        }
    }
}