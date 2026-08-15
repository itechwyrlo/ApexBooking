using System;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CompleteBooking
{
    public record CompleteBookingCommand(Guid BookingId) : ICommand;
}
