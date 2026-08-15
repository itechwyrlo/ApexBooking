using ApexBooking.Core.Application.Dtos.Response;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.CheckoutBooking
{
    public record CheckoutBookingCommand(Guid BookingId) : ICommand<CheckoutBookingResult>;
}
