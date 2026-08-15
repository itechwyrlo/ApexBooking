using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetTeamMemberRemovalImpact
{
    public record GetTeamMemberRemovalImpactQuery(Guid TenantMemberId) : IQuery<TeamMemberRemovalImpact>;
}
