using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetTenantPaymentPolicy;

internal sealed class GetTenantPaymentPolicyQueryHandler
    : IQueryHandler<GetTenantPaymentPolicyQuery, BaseResponse<TenantPaymentPolicyDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _contextService;

    public GetTenantPaymentPolicyQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
    }

    public async Task<BaseResponse<TenantPaymentPolicyDto>> Handle(
        GetTenantPaymentPolicyQuery query,
        CancellationToken ct)
    {
        var tenantId = _contextService.GetCurrentTenantId();

        var tenant = await _unitOfWork.TenantRepository.GetAsync(
            predicate: t => t.TenantId == tenantId,
            includes: t => t.TenantPaymentPolicy);

        if (tenant is null)
            return BaseResponse<TenantPaymentPolicyDto>.Failure("Tenant not found.");

        var dto = tenant.TenantPaymentPolicy is not null
            ? tenant.TenantPaymentPolicy.ToPaymentPolicyDto()
            : TenantMappings.DefaultPaymentPolicyDto();

        return BaseResponse<TenantPaymentPolicyDto>.Success(dto);
    }
}
