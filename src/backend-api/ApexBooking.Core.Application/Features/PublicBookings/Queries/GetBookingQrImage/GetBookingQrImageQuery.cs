using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetBookingQrImage
{
    public record GetBookingQrImageQuery(string TicketToken) : IQuery<byte[]>;
}
