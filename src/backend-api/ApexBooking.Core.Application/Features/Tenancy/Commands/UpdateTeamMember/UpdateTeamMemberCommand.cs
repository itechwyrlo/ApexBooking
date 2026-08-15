using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.UpdateTeamMember
{
    // Email is intentionally absent — see TenantMember.UpdateProfile for why it isn't editable here.
    public record UpdateTeamMemberCommand(
        Guid TenantMemberId,
        string FirstName,
        string LastName,
        string ContactNumber,
        string? CustomJobTitle,
        SystemRole Role
    ) : ICommand;
}
