using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetAllTeam
{
    public record GetAllTeamQuery(QueryObjectParams param) : IQuery<QueryResult<TeamMemberSummary>>;
}