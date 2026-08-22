using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Bookings.Commands.ProcessPaymentWebhook
{
    public record ProcessPaymentWebhookCommand(
        string RemarksToken,
        string? PayMongoPaymentId,
        string RawBody,
        string? SignatureHeader,
        string PayMongoEventId,
        long? PaidAmountInCentavos) : ICommand;
}
