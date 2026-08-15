using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Platform.Queries.GetDashboardSummary
{
    public record GetPlatformDashboardSummaryQuery() : IQuery<PlatformDashboardSummary>;
}
