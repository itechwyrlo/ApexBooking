using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetBookingById
{
    public sealed record GetBookingByIdQuery(Guid BookingId) : IQuery<BookingDetailDto>;
}