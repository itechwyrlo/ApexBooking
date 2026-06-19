using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetPlatformOverview;

internal sealed class GetPlatformOverviewQueryHandler
    : IQueryHandler<GetPlatformOverviewQuery, PlatformOverviewDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPlatformOverviewQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PlatformOverviewDto> Handle(
        GetPlatformOverviewQuery query,
        CancellationToken ct)
    {
        var tenants = (await _unitOfWork.TenantRepository
            .GetAllAsync(t => t.Users))
            .ToList();

        var orgs = tenants
            .Select(t => t.ToOrganizationSummaryDto(t.Users.Count))
            .OrderByDescending(o => o.CreatedAt)
            .ToList();

        return orgs.ToPlatformOverviewDto();
    }
}
