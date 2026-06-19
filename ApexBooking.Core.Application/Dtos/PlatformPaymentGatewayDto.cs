using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos;

public record PlatformPaymentGatewayDto(
    Guid Id,
    GatewayProvider GatewayProvider,
    GatewayMode Mode,
    bool IsActive,
    DateTime? ValidatedAt
);
