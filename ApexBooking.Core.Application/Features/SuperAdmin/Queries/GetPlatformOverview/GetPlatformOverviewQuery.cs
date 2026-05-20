using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetPlatformOverview;

public sealed record GetPlatformOverviewQuery() : IQuery<PlatformOverviewDto>;
