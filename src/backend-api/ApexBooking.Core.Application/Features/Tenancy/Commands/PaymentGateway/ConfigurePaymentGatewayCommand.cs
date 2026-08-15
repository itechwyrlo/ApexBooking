using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Commands.PaymentGateway
{
    public record ConfigurePaymentGatewayCommand(
        string SecretKey,
        string PublicKey
    ) : ICommand;
}
