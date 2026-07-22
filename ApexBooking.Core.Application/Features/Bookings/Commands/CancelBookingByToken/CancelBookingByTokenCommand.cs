using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBookingByToken
{
    public sealed record CancelBookingByTokenCommand(string Token, string? Reason)
        : ICommand;
}
