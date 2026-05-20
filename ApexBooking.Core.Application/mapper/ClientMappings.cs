using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Domain.Entities;

namespace ApexBooking.Core.Application.mapper
{
    public static class ClientMappings
    {
        public static ClientBookingDto ToClientBookingDto(this Booking booking) => new(
            booking.BookingId.Value,
            booking.BookingReference,
            booking.ServiceName,
            booking.ResourceName,
            booking.ScheduledDate,
            booking.ScheduledStartTime,
            booking.ScheduledEndTime,
            booking.Status,
            booking.PriceSnapshot,
            booking.CurrencyCode);
    }
}
