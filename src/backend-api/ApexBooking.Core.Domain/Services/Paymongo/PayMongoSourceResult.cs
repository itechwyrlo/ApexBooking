using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ApexBooking.Core.Domain.Services.Paymongo
{
    public record PayMongoSourceResult(
        string QrCodeUrl,      // Renders the direct scan canvas box on the wizard
        string CheckoutUrl     // General interactive GCash/Maya landing portal
    );
}