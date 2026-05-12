using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetPlatformOverview;

internal sealed class GetPlatformOverviewQueryHandler
    : IQueryHandler<GetPlatformOverviewQuery, BaseResponse<PlatformOverviewDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPlatformOverviewQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<PlatformOverviewDto>> Handle(
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

        return BaseResponse<PlatformOverviewDto>.Success(orgs.ToPlatformOverviewDto());
    }
}
