using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.MarkBookingNoShow
{
    public record MarkBookingNoShowCommand(Guid BookingId) : ICommand;
}
