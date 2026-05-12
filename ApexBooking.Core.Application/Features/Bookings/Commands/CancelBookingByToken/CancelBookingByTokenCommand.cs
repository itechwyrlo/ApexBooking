using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CancelBookingByToken
{
    public sealed record CancelBookingByTokenCommand(string Token, string? Reason)
        : ICommand<BaseResponse<bool>>;
}
