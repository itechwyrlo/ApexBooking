using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantRevenue
{
    public record GetTenantRevenueQuery(DateOnly FromDate, DateOnly ToDate) : IQuery<TenantRevenueDto>;
}
