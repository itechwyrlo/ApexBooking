using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetTenantPaymentPolicy;

internal sealed class GetTenantPaymentPolicyQueryHandler
    : IQueryHandler<GetTenantPaymentPolicyQuery, TenantPaymentPolicyDto>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _contextService;

    public GetTenantPaymentPolicyQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
    }

    public async Task<TenantPaymentPolicyDto> Handle(GetTenantPaymentPolicyQuery query, CancellationToken ct)
    {
        var tenantId = _contextService.GetCurrentTenantId();

        var tenant = await _unitOfWork.TenantRepository.GetAsync(
            predicate: t => t.TenantId == tenantId,
            includes: t => t.TenantPaymentPolicy);

        if (tenant is null)
            throw new NotFoundException("Tenant not found.");

        return tenant.TenantPaymentPolicy is not null
            ? tenant.TenantPaymentPolicy.ToPaymentPolicyDto()
            : TenantMappings.DefaultPaymentPolicyDto();
    }
}
