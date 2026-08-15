using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentGateway
{
    public record SetPaymentGatewayWebhookSecretCommand(string WebhookSecret) : ICommand;
}
