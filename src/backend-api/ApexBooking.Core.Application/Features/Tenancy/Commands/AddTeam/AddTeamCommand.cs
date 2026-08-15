using ApexBooking.Core.Application.Dtos.Request;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.AddTeam
{
    public record AddTeamCommand(AddTeamMemberRequest request) : ICommand;
}