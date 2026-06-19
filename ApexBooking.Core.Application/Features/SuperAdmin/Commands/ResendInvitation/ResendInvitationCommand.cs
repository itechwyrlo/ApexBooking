using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ResendInvitation;

public sealed record ResendInvitationCommand(
    string TenantSlug,
    Guid UserId
) : ICommand;
