using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Public.Queries.GetPublicServices;

internal sealed class GetPublicServicesQueryHandler
    : IQueryHandler<GetPublicServicesQuery, BaseResponse<List<PublicServiceDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPublicServicesQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<List<PublicServiceDto>>> Handle(
        GetPublicServicesQuery query,
        CancellationToken cancellationToken)
    {
        var tenant = await _unitOfWork.TenantRepository.FindBySlugAsync(query.Slug);

        if (tenant is null)
            return BaseResponse<List<PublicServiceDto>>.Failure($"Tenant '{query.Slug}' not found.");

        var services = await _unitOfWork.ServiceRepository
            .GetActiveServicesByTenantAsync(tenant.TenantId, cancellationToken);

        var dtos = services.Select(s => new PublicServiceDto(
            ServiceId: s.ServiceId.Value,
            Name: s.Name,
            Description: s.Description,
            DurationMinutes: s.DurationMinutes,
            Price: s.Price,
            CurrencyCode: s.CurrencyCode
        )).ToList();

        return BaseResponse<List<PublicServiceDto>>.Success(dtos);
    }
}