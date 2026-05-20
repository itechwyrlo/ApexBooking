using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetPlatformPaymentGateway;

public sealed record GetPlatformPaymentGatewayQuery() : IQuery<PlatformPaymentGatewayDto?>;
