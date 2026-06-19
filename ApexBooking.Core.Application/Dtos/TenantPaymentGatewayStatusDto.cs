using ApexBooking.Core.Domain.Enums;

namespace ApexBooking.Core.Application.Dtos;

public record TenantPaymentGatewayStatusDto(
    GatewayProvider? GatewayProvider,
    GatewayMode? Mode,
    bool IsActive,
    DateTime? ValidatedAt
);
