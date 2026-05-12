using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Application.Services.Mappings;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicServices;

internal sealed class GetPublicServicesQueryHandler
    : IQueryHandler<GetPublicServicesQuery, PagedResult<PublicServiceDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPublicServicesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PagedResult<PublicServiceDto>> Handle(
        GetPublicServicesQuery query,
        CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);

        var services = await _unitOfWork.ServiceRepository.GetPageAsync(
            query.param,
            predicate: s => s.TenantId == tenant.TenantId && s.IsActive);
            
        var dto = services.data.Select(s => s.ToPublicServiceDto()).ToList();

        return new PagedResult<PublicServiceDto>(dto, services.total);
    }
}