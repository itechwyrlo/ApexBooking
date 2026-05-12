using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Settings.Commands.UpdateTenantPaymentPolicy;

internal sealed class UpdateTenantPaymentPolicyCommandHandler
    : ICommandHandler<UpdateTenantPaymentPolicyCommand, BaseResponse<TenantPaymentPolicyDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _contextService;

    public UpdateTenantPaymentPolicyCommandHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
    }

    public async Task<BaseResponse<TenantPaymentPolicyDto>> Handle(
        UpdateTenantPaymentPolicyCommand command,
        CancellationToken ct)
    {
        var tenantId = _contextService.GetCurrentTenantId();

        var tenant = await _unitOfWork.TenantRepository.GetAsync(
            predicate: t => t.TenantId == tenantId,
            includes: t => t.TenantPaymentPolicy);

        if (tenant is null)
            return BaseResponse<TenantPaymentPolicyDto>.Failure("Tenant not found.");

        // Lazy-create on first save for tenants that predate this feature
        if (tenant.TenantPaymentPolicy is null)
            tenant.CreateTenantPaymentPolicy();

        tenant.TenantPaymentPolicy!.UpdatePolicy(
            paymentRequired: command.PaymentRequired,
            depositOnly: command.DepositOnly,
            depositType: command.DepositType,
            depositValue: command.DepositValue,
            refundPercent: command.RefundPercent);

        await _unitOfWork.CompleteAsync(ct);

        return BaseResponse<TenantPaymentPolicyDto>.Success(tenant.TenantPaymentPolicy.ToPaymentPolicyDto());
    }
}
