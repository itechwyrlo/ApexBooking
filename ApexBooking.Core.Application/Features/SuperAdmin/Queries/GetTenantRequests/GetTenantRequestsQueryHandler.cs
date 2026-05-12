using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.mapper;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Entities;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.SuperAdmin.Queries.GetTenantRequests;

internal sealed class GetTenantRequestsQueryHandler
    : IQueryHandler<GetTenantRequestsQuery, BaseResponse<IEnumerable<TenantRequestDto>>>
{
    private readonly IUnitOfWork _unitOfWork;

    public GetTenantRequestsQueryHandler(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<BaseResponse<IEnumerable<TenantRequestDto>>> Handle(
        GetTenantRequestsQuery request,
        CancellationToken cancellationToken)
    {
        IEnumerable<TenantRequest> requests;

        if (!string.IsNullOrWhiteSpace(request.Status) &&
            Enum.TryParse<TenantRequestStatus>(request.Status, ignoreCase: true, out var statusFilter))
        {
            requests = await _unitOfWork.TenantRequestRepository.GetAllAsync(
                r => r.Status == statusFilter);
        }
        else
        {
            requests = await _unitOfWork.TenantRequestRepository.GetAllAsync();
        }

        var dtos = requests
            .OrderByDescending(r => r.CreatedAt)
            .Select(r => r.ToDto());

        return BaseResponse<IEnumerable<TenantRequestDto>>.Success(dtos);
    }
}
