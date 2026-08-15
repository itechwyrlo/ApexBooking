using ApexBooking.Core.Application.Dtos.Request;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.TenantRequest.Queries.GetAllRequest
{
    public record GetAllPendingRequestQuery(QueryObjectParams param) : IQuery<QueryResult<PendingTenantRequestSummary>>;
}