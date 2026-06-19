using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetCalendarBookings;

public sealed record GetCalendarBookingsQuery(int Year, int Month)
    : IQuery<IReadOnlyList<TenantBookingsDto>>;
