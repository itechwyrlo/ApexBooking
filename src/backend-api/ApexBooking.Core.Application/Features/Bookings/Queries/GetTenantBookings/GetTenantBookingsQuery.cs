using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetTenantBookings
{
    public record GetTenantBookingsQuery(
        QueryObjectParams param,
        Guid? BranchId = null,
        Guid? StaffId = null,
        BookingStatus? Status = null,
        DateOnly? FromDate = null,
        DateOnly? ToDate = null
    ) : IQuery<QueryResult<TenantBookingSummary>>;
}
