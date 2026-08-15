using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Commands.CancelBookingByToken
{
    public record CancelBookingByTokenCommand(
        string Token,
        string? Reason,
        string? EwalletProvider,
        string? EwalletNumber,
        string? EwalletName
    ) : ICommand;
}
