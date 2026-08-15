using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.RecordPayInVisitPayment
{
    public record RecordPayInVisitPaymentCommand(Guid BookingId) : ICommand;
}
