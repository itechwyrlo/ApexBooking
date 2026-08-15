using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Platform.Queries.GetPlatformBookings
{
    public record GetPlatformBookingsQuery(
        QueryObjectParams Param,
        Guid? TenantId,
        BookingStatus? Status,
        DateOnly? FromDate,
        DateOnly? ToDate
    ) : IQuery<QueryResult<PlatformBookingSummary>>;
}
