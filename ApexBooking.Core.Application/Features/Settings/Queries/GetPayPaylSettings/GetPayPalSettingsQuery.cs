using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetPayPaylSettings;

public sealed record GetPayPalSettingsQuery() : IQuery<TenantPaymentGatewayStatusDto>;
