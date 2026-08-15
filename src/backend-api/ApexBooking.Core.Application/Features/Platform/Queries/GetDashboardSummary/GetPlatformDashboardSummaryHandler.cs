using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Repositories;

namespace ApexBooking.Core.Application.Features.Platform.Queries.GetDashboardSummary
{
    public class GetPlatformDashboardSummaryHandler
        : IQueryHandler<GetPlatformDashboardSummaryQuery, PlatformDashboardSummary>
    {
        private readonly IPlatformQueries _platformQueries;

        public GetPlatformDashboardSummaryHandler(IPlatformQueries platformQueries) => _platformQueries = platformQueries;

        public async Task<PlatformDashboardSummary> Handle(
            GetPlatformDashboardSummaryQuery query,
            CancellationToken cancellationToken)
        {
            var counts = await _platformQueries.GetDashboardCountsAsync(cancellationToken);

            return new PlatformDashboardSummary(
                counts.TotalTenants,
                counts.ActiveTenants,
                counts.PendingRequests,
                counts.ApprovedRequests,
                counts.RejectedRequests,
                counts.BookingsToday,
                counts.BookingsThisMonth);
        }
    }
}
