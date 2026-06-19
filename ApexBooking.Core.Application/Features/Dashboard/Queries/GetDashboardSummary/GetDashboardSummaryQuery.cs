using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Dashboard.Queries.GetDashboardSummary
{
    public sealed record GetDashboardSummaryQuery() : IQuery<DashboardSummaryDto>;
}
