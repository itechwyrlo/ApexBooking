using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Payment.Commands.HandlePayPalWebhook
{
    public sealed record HandlePayPalWebhookCommand(
        string RequestBody,
        string TransmissionId,
        string TransmissionTime,
        string CertUrl,
        string AuthAlgo,
        string TransmissionSig
    ) : ICommand;
}