using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.RemoveTeamMember
{
    public record RemoveTeamMemberCommand(Guid TenantMemberId) : ICommand<TeamMemberRemovalResult>;
}
