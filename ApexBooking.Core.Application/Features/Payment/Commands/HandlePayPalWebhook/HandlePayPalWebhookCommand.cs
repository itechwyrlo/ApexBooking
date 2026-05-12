using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Payment.Commands.HandlePayPalWebhook
{
    public sealed record HandlePayPalWebhookCommand(
        string RequestBody,
        string TransmissionId,
        string TransmissionTime,
        string CertUrl,
        string AuthAlgo,
        string TransmissionSig
    ) : ICommand<BaseResponse<bool>>;
}