using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Domain.ValueObjects;
using ApexBooking.SharedKernel.Exceptions;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetTenantRequestById;

internal sealed class GetTenantRequestByIdQueryHandler
    : IQueryHandler<GetTenantRequestByIdQuery, TenantRequestDetailDto>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTenantRequestByIdQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<TenantRequestDetailDto> Handle(
        GetTenantRequestByIdQuery request,
        CancellationToken cancellationToken)
    {
        var tenantRequest = await _unitOfWork.TenantRequestRepository.GetAsync(
            r => r.Id == new TenantRequestId(request.Id));

        if (tenantRequest is null)
            throw new NotFoundException("Request not found.");

        return tenantRequest.ToDetailDto();
    }
}
