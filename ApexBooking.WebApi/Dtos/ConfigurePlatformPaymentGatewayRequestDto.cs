using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.WebApi.Dtos;

public record ConfigurePlatformPaymentGatewayRequestDto(
    GatewayProvider GatewayProvider,
    string ClientId,
    string SecretKey,
    string WebhookId,
    GatewayMode Mode
);
