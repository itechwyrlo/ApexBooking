using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ConfirmBooking;

public sealed record ConfirmBookingCommand(Guid BookingId) : ICommand;
