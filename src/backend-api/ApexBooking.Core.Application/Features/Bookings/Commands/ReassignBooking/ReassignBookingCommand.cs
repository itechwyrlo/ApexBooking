using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ReassignBooking
{
    public record ReassignBookingCommand(Guid BookingId, Guid NewStaffId) : ICommand;
}
