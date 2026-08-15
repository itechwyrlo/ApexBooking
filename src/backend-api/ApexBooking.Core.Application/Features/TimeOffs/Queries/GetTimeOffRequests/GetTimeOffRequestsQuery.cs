using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.TimeOffs.Queries.GetTimeOffRequests
{
    // Access scope is enforced server-side, not by this parameter: Staff callers are always
    // restricted to their own requests regardless of what's sent; Owner/Admin see the whole team.
    public record GetTimeOffRequestsQuery(
        QueryObjectParams Param,
        TimeOffStatus? Status = null
    ) : IQuery<QueryResult<TimeOffRequestSummary>>;
}
