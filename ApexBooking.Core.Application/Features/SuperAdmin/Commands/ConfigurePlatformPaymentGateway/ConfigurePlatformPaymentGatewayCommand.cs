using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Enums;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Commands.ConfigurePlatformPaymentGateway;

public sealed record ConfigurePlatformPaymentGatewayCommand(
    GatewayProvider GatewayProvider,
    string ClientId,
    string SecretKey,
    string WebhookId,
    GatewayMode Mode
) : ICommand<BaseResponse<PlatformPaymentGatewayDto>>;
