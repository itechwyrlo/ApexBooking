using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ProcessPaymentWebhook
{
    public record ProcessPaymentWebhookCommand(string RemarksToken, string? PayMongoPaymentId, string RawBody, string? SignatureHeader) : ICommand;
}