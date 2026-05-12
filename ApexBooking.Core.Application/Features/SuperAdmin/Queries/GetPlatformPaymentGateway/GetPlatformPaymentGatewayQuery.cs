using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetPlatformPaymentGateway;

public sealed record GetPlatformPaymentGatewayQuery() : IQuery<BaseResponse<PlatformPaymentGatewayDto>>;
