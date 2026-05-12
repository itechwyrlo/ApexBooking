using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetPlatformOverview;

public sealed record GetPlatformOverviewQuery() : IQuery<BaseResponse<PlatformOverviewDto>>;
