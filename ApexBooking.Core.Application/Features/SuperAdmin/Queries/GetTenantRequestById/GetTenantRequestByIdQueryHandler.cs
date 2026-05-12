using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetTenantRequestById;

internal sealed class GetTenantRequestByIdQueryHandler
    : IQueryHandler<GetTenantRequestByIdQuery, BaseResponse<TenantRequestDetailDto>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTenantRequestByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<TenantRequestDetailDto>> Handle(
        GetTenantRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var tenantRequest = await _unitOfWork.TenantRequestRepository.GetAsync(
            r => r.Id == new TenantRequestId(request.Id));

        if (tenantRequest is null)
            return BaseResponse<TenantRequestDetailDto>.Failure("Request not found.");

        return BaseResponse<TenantRequestDetailDto>.Success(tenantRequest.ToDetailDto());
    }
}
