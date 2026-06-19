using System.Threading;
using System.Threading.Tasks;
using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.Settings.Queries.GetPayPaylSettings;

internal sealed class GetPayPalSettingsHandler
    : IQueryHandler<GetPayPalSettingsQuery, TenantPaymentGatewayStatusDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPayPalSettingsHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantPaymentGatewayStatusDto> Handle(GetPayPalSettingsQuery query, CancellationToken ct)
    {
        var gateway = await _unitOfWork.PlatformPaymentGatewayRepository.GetActiveAsync(ct);

        if (gateway is null)
            return TenantMappings.DefaultPaymentGatewayStatusDto();

        return gateway.ToPaymentGatewayStatusDto();
    }
}
