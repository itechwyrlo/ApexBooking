using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetPayPaylSettings;

internal sealed class GetPayPalSettingsHandler
    : IQueryHandler<GetPayPalSettingsQuery, BaseResponse<TenantPaymentGatewayStatusDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPayPalSettingsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<TenantPaymentGatewayStatusDto>> Handle(
        GetPayPalSettingsQuery query,
        CancellationToken ct)
    {
        var gateway = await _unitOfWork.PlatformPaymentGatewayRepository.GetActiveAsync(ct);

        if (gateway is null)
            return BaseResponse<TenantPaymentGatewayStatusDto>.Success(
                TenantMappings.DefaultPaymentGatewayStatusDto());

        return BaseResponse<TenantPaymentGatewayStatusDto>.Success(gateway.ToPaymentGatewayStatusDto());
    }
}
