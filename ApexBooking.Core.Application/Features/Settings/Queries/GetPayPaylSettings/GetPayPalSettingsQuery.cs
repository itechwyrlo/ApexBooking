using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetPayPaylSettings;

public sealed record GetPayPalSettingsQuery() : IQuery<BaseResponse<TenantPaymentGatewayStatusDto>>;
