using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Services.Ticketing;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.PublicBookings.Queries.GetBookingQrImage
{
    // No booking/tenant lookup needed — the image is just a rendering of the token itself.
    // Validating the signature is enough to confirm it's a real, unforged token before
    // spending the work to draw it; whether the booking it points to still exists is checked
    // wherever the token is actually redeemed (scan-arrival, status poll), not here.
    public class GetBookingQrImageHandler : IQueryHandler<GetBookingQrImageQuery, byte[]>
    {
        private readonly ITicketTokenService _ticketTokenService;
        private readonly IQrCodeGenerator _qrCodeGenerator;

        public GetBookingQrImageHandler(ITicketTokenService ticketTokenService, IQrCodeGenerator qrCodeGenerator)
        {
            _ticketTokenService = ticketTokenService;
            _qrCodeGenerator = qrCodeGenerator;
        }

        public Task<byte[]> Handle(GetBookingQrImageQuery query, CancellationToken cancellationToken)
        {
            if (!_ticketTokenService.TryValidate(query.TicketToken, out _))
                throw new BusinessRuleBrokenException("This booking ticket is invalid or could not be verified.");

            return Task.FromResult(_qrCodeGenerator.GeneratePng(query.TicketToken));
        }
    }
}
