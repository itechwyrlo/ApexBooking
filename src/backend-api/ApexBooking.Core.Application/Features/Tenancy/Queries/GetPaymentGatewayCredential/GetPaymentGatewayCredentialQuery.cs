using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Tenancy.Queries.GetPaymentGatewayCredential
{
    public record PaymentGatewayCredentialDto(
        string? PublicKey,
        string? MaskedSecretKey,
        string? Mode,
        bool IsConfigured,
        bool IsWebhookConfigured
    );

    public record GetPaymentGatewayCredentialQuery() : IQuery<PaymentGatewayCredentialDto>;
}
