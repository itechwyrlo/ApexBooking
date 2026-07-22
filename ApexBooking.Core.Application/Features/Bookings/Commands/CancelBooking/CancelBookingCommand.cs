using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking
{
    public sealed record CancelBookingCommand(
        Guid BookingId,
        string? Reason
    ) : ICommand;
}