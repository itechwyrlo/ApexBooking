using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Repositories;
using ApexBooking.SharedKernel.Models;
using static ApexBooking.SharedKernel.ValueObject.ValueObjectTenantIdentifier;

namespace ApexBooking.Core.Application.Features.Platform.Queries.GetPlatformBookings
{
    public class GetPlatformBookingsHandler
        : IQueryHandler<GetPlatformBookingsQuery, QueryResult<PlatformBookingSummary>>
    {
        private readonly IPlatformQueries _platformQueries;

        public GetPlatformBookingsHandler(IPlatformQueries platformQueries) => _platformQueries = platformQueries;

        public async Task<QueryResult<PlatformBookingSummary>> Handle(
            GetPlatformBookingsQuery query,
            CancellationToken cancellationToken)
        {
            var pagedResult = await _platformQueries.GetBookingsPageAsync(
                query.Param,
                query.TenantId is not null ? new TenantId(query.TenantId.Value) : null,
                query.Status,
                query.FromDate,
                query.ToDate,
                cancellationToken);

            var mappedItems = pagedResult.data.Select(row => new PlatformBookingSummary(
                row.BookingId,
                row.BookingReference,
                row.TenantId,
                row.TenantName,
                row.CustomerName,
                row.StaffName,
                row.ServiceName,
                row.ScheduledDate,
                row.ScheduledStartTime,
                row.Status.ToString()));

            return new QueryResult<PlatformBookingSummary>(mappedItems, pagedResult.total);
        }
    }
}
