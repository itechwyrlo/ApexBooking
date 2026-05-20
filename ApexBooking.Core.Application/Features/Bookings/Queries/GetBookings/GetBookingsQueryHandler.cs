using ApexBooking.Core.Application.Dtos;
using ApexBooking.Core.Application.Messaging.Abstractions;
using ApexBooking.Core.Domain.Interfaces;
using ApexBooking.Core.Application.mapper;
using ApexBooking.SharedKernel.Models;

namespace ApexBooking.Core.Application.Features.Bookings.Queries.GetBookings;

internal sealed class GetBookingsQueryHandler
    : IQueryHandler<GetBookingsQuery, PagedResult<TenantBookingsDto>>
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IUserContextService _contextService;

    public GetBookingsQueryHandler(IUnitOfWork unitOfWork, IUserContextService contextService)
    {
        _unitOfWork = unitOfWork;
        _contextService = contextService;
    }

    public async Task<PagedResult<TenantBookingsDto>> Handle(
        GetBookingsQuery query,
        CancellationToken cancellationToken)
    {
        var sortedParams = query.param with
        {
            SortingParams = query.param.SortingParams.Count > 0
                ? query.param.SortingParams
                : [new SortParam { OrderProperty = "CreatedAt", SortOrderDescending = true }]
        };

        if (_contextService.GetUserRole() == "staff")
        {
            var userId = _contextService.GetCurrentUserId();
            var staff = await _unitOfWork.StaffRepository
                .FindByUserIdAsync(userId, cancellationToken)
                .ConfigureAwait(false);

            if (staff is null)
                throw new UnauthorizedAccessException("Staff profile not found.");

            var staffResult = await _unitOfWork.BookingRepository
                .GetPageAsync(sortedParams, predicate: b => b.StaffId == staff.StaffId, b => b.Guest)
                .ConfigureAwait(false);

            return new PagedResult<TenantBookingsDto>(
                staffResult.data.Select(b => b.ToTenantDto()).ToList(),
                staffResult.total);
        }

        var pagedResult = await _unitOfWork.BookingRepository
            .GetPageAsync(sortedParams, predicate: null, b => b.Guest)
            .ConfigureAwait(false);

        return new PagedResult<TenantBookingsDto>(
            pagedResult.data.Select(b => b.ToTenantDto()).ToList(),
            pagedResult.total);
    }
}
