using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBooking
{
    public record CancelBookingCommand(
        Guid BookingId,
        string Reason,
        string? EwalletProvider,
        string? EwalletNumber,
        string? EwalletName
    ) : ICommand;
}
