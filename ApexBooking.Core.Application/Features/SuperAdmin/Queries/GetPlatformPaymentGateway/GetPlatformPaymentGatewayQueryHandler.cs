using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetPlatformPaymentGateway;

internal sealed class GetPlatformPaymentGatewayQueryHandler
    : IQueryHandler<GetPlatformPaymentGatewayQuery, PlatformPaymentGatewayDto?>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetPlatformPaymentGatewayQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<PlatformPaymentGatewayDto?> Handle(
        GetPlatformPaymentGatewayQuery query,
        CancellationToken ct)
    {
        var gateway = await _unitOfWork.PlatformPaymentGatewayRepository.GetActiveAsync(ct);

        if (gateway is null)
            return null;

        return gateway.ToPlatformPaymentGatewayDto();
    }
}
