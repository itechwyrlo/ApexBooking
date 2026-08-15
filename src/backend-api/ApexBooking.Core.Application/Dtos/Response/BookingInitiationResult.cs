using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Application.Dtos.Response
{
    public record BookingInitiationResult(
        Guid BookingId,
        string BookingReference,
        bool RequiresPayment,
        decimal AmountToPay,
        string? PayMongoQrCodeUrl,
        string? PayMongoCheckoutUrl,
        string TicketToken,
        string TicketQrCodeDataUri
    );
}