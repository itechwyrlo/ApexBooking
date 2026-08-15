using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.SetBookingStaffNotes
{
    public record SetBookingStaffNotesCommand(Guid BookingId, string Notes) : ICommand;
}
