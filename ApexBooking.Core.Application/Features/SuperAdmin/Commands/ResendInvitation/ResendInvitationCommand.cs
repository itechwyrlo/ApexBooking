using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ResendInvitation;

public sealed record ResendInvitationCommand(
    string TenantSlug,
    Guid UserId
) : ICommand<BaseResponse<bool>>;
